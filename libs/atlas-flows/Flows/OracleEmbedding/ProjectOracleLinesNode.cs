using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using Flowthru.Step;

namespace MagicAtlas.Flows.OracleEmbedding;

/// <summary>
/// Projects card oracle text into a clean per-line inventory for the embedding pipeline, in a
/// single pass over the card corpus. Emits three outputs:
/// </summary>
/// <list type="number">
/// <item><see cref="KeywordVocabulary"/> — the sorted distinct set of Scryfall keywords seen
/// across all cards. Consumed downstream by the keyword-cluster report.</item>
/// <item><c>IEnumerable&lt;OracleLine&gt;</c> — one row per surviving oracle-text line plus one
/// synthetic row per (card, keyword) pair from Scryfall metadata.</item>
/// <item><see cref="BarrelDetectionReport"/> — diagnostic of which oracle lines were classified
/// as keyword barrels (all comma-separated segments matched a keyword) and dropped.</item>
/// </list>
/// <remarks>
/// <para>
/// Two-pass within each card: classify natural lines (drop barrels, keep plain &amp; borderline)
/// then emit one synthetic line per keyword in <see cref="CardCoreData.Keywords"/>.
/// </para>
/// <para>
/// LineIds are deterministic SHA-256 hashes — <c>"oracle:{card_id}:{text}"</c> for natural lines,
/// <c>"keyword:{card_id}:{keyword_normalized}"</c> for synthetic. Distinct namespaces prevent
/// collision between a natural-text line and a synthetic-keyword line that happen to share text.
/// </para>
/// </remarks>
[FlowthruStep]
public static partial class ProjectOracleLinesNode
{
  /// <summary>Caps on barrel/borderline sample arrays in the diagnostic report.</summary>
  private const int BarrelExampleSampleSize = 50;
  private const int BorderlineExampleSampleSize = 50;

  // Strips trailing magnitude/parameter from a keyword segment: " 4", " {2}", " {X}".
  [GeneratedRegex(@"\s+(\{[X0-9]+\}|\d+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
  private static partial Regex MagnitudeSuffixPattern();

  // Matches parameterized keyword variants: "protection from X", "hexproof from X", "ward from X".
  // Conservatively bounded to the small set of MTG keywords that take a "from" parameter.
  [GeneratedRegex(@"^(protection|hexproof|ward) from \S", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
  private static partial Regex ParameterizedKeywordPattern();

  // Sentence-level structure markers — if any segment of a candidate line has these, it's
  // ability text, not a compound keyword-variant list. Activated-ability costs ("{T}:",
  // "{2}:"), modal markers ("Choose"), trigger words ("When", "Whenever", "At the beginning"),
  // and conditional clauses ("As long as", "If") all disqualify.
  [GeneratedRegex(
    @"[:.(]|\{[A-Z0-9X/]+\}|\b(when|whenever|at the beginning|if|as long|target|enchanted|equipped|each|create|put|deals|gain|lose|choose)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase)]
  private static partial Regex SentenceStructurePattern();

  private const int CompoundLineMaxLength = 80;

  public static Func<
    IEnumerable<CardCoreData>,
    Task<(KeywordVocabulary, IEnumerable<OracleLine>, BarrelDetectionReport)>
  > Create() =>
    cards => Task.FromResult<(KeywordVocabulary, IEnumerable<OracleLine>, BarrelDetectionReport)>(
      ProjectCards(cards.ToList())
    );

  private static (KeywordVocabulary, List<OracleLine>, BarrelDetectionReport) ProjectCards(
    IReadOnlyList<CardCoreData> cards
  )
  {
    // ── Pass 0: build the keyword vocabulary from card.Keywords. Canonical Scryfall casing
    // (e.g. "Flying", "Ward") preserved in the catalog item; case-folded copy used for
    // matching inside the classifier.
    var vocabulary = cards
      .Where(c => c.Keywords is not null)
      .SelectMany(c => c.Keywords!)
      .Distinct(StringComparer.Ordinal)
      .OrderBy(k => k, StringComparer.Ordinal)
      .ToList();
    var keywordSet = vocabulary
      .Select(NormalizeKeywordSegment)
      .ToHashSet(StringComparer.Ordinal);

    var lines = new List<OracleLine>(capacity: cards.Count * 4);
    var barrels = new List<BarrelExample>();
    var borderlines = new List<BorderlineExample>();
    var extendedBarrels = new List<ExtendedBarrelExample>();
    int totalConsidered = 0;
    int barrelLinesDropped = 0;
    int borderlineLines = 0;
    int extendedBarrelLines = 0;
    int extendedBarrelSegmentsEmitted = 0;
    int syntheticAdded = 0;

    foreach (var card in cards)
    {
      // ── Pass 1: natural oracle lines (parentheticals already stripped upstream).
      // Track the surviving-line ordinal within this card so split/transform cards with
      // repeated lines (e.g. "Target creature gets +3/+3" appearing on both faces) get
      // distinct LineIds rather than colliding.
      var rawText = card.OracleText;
      int lineSeq = 0;
      if (!string.IsNullOrWhiteSpace(rawText))
      {
        foreach (var rawLine in rawText.Split('\n'))
        {
          var line = rawLine.Trim();
          if (string.IsNullOrWhiteSpace(line)) continue;
          totalConsidered++;

          var classification = ClassifyLine(line, keywordSet);

          switch (classification.Kind)
          {
            case LineKind.Barrel:
              barrelLinesDropped++;
              if (barrels.Count < BarrelExampleSampleSize)
              {
                barrels.Add(new BarrelExample
                {
                  CardId = card.Id,
                  CardName = card.Name,
                  Line = line,
                  Segments = classification.NormalizedSegments,
                });
              }
              break;

            case LineKind.Borderline:
              borderlineLines++;
              if (borderlines.Count < BorderlineExampleSampleSize)
              {
                borderlines.Add(new BorderlineExample
                {
                  CardId = card.Id,
                  CardName = card.Name,
                  Line = line,
                  MatchedSegments = classification.NormalizedSegments,
                });
              }
              // Borderline lines DO survive — only full barrels get stripped.
              lines.Add(new OracleLine
              {
                LineId = StableLineId("oracle", card.Id, lineSeq, line),
                CardId = card.Id,
                OracleId = card.OracleId,
                Text = line,
              });
              lineSeq++;
              break;

            case LineKind.ExtendedBarrel:
              extendedBarrelLines++;
              // Split into one OracleLine per non-bare-keyword segment. Bare keyword segments
              // are dropped because the synthetic-keyword emission below already covers them.
              var emittedSegments = new List<string>();
              foreach (var rawSeg in line.Split(','))
              {
                var segText = rawSeg.Trim();
                if (segText.Length == 0) continue;
                var normalizedSeg = NormalizeKeywordSegment(rawSeg);
                if (keywordSet.Contains(normalizedSeg)) continue;
                lines.Add(new OracleLine
                {
                  LineId = StableLineId("compound", card.Id, lineSeq, segText),
                  CardId = card.Id,
                  OracleId = card.OracleId,
                  Text = segText,
                });
                lineSeq++;
                emittedSegments.Add(segText);
                extendedBarrelSegmentsEmitted++;
              }
              if (extendedBarrels.Count < BorderlineExampleSampleSize)
              {
                extendedBarrels.Add(new ExtendedBarrelExample
                {
                  CardId = card.Id,
                  CardName = card.Name,
                  Line = line,
                  EmittedSegments = emittedSegments,
                });
              }
              break;

            case LineKind.Plain:
              lines.Add(new OracleLine
              {
                LineId = StableLineId("oracle", card.Id, lineSeq, line),
                CardId = card.Id,
                OracleId = card.OracleId,
                Text = line,
              });
              lineSeq++;
              break;
          }
        }
      }

      // ── Pass 2: synthetic per-keyword lines. One per Scryfall keyword on this card,
      // emitted in the canonical Scryfall casing. The encoder dedups across cards so the
      // marginal encode cost per unique keyword is zero on warm runs.
      if (card.Keywords is { Count: > 0 })
      {
        foreach (var keyword in card.Keywords)
        {
          var canonical = keyword.Trim();
          if (canonical.Length == 0) continue;

          lines.Add(new OracleLine
          {
            // Synthetic line ids use the keyword name (lowercased) as the seq discriminator
            // since a card's Scryfall keyword list shouldn't contain duplicates.
            LineId = StableLineId("keyword", card.Id, 0, canonical.ToLowerInvariant()),
            CardId = card.Id,
            OracleId = card.OracleId,
            Text = canonical,
          });
          syntheticAdded++;
        }
      }
    }

    var report = new BarrelDetectionReport
    {
      TotalLinesConsidered = totalConsidered,
      BarrelLinesDropped = barrelLinesDropped,
      BorderlineLines = borderlineLines,
      ExtendedBarrelLines = extendedBarrelLines,
      ExtendedBarrelSegmentsEmitted = extendedBarrelSegmentsEmitted,
      SyntheticKeywordLinesAdded = syntheticAdded,
      Barrels = barrels,
      Borderlines = borderlines,
      ExtendedBarrels = extendedBarrels,
    };
    return (new KeywordVocabulary { Keywords = vocabulary }, lines, report);
  }

  private enum LineKind { Plain, Borderline, ExtendedBarrel, Barrel }

  private record LineClassification(LineKind Kind, List<string> NormalizedSegments);

  /// <summary>
  /// A line is a <c>Barrel</c> iff every comma-separated segment normalizes to a known keyword.
  /// It's an <c>ExtendedBarrel</c> iff every segment is either a known keyword or a
  /// parameterized keyword variant (e.g. <c>"protection from green"</c>) AND the line has no
  /// sentence-level structure — these are compound keyword-variant lists that should be split.
  /// It's <c>Borderline</c> iff some-but-not-all segments match and it doesn't qualify as
  /// extended (long ability text containing keywords). Otherwise <c>Plain</c>.
  /// </summary>
  private static LineClassification ClassifyLine(string line, HashSet<string> keywordSet)
  {
    var segments = line.Split(',');
    var matched = new List<string>(capacity: segments.Length);
    int matchCount = 0;
    foreach (var seg in segments)
    {
      var normalized = NormalizeKeywordSegment(seg);
      if (normalized.Length > 0 && keywordSet.Contains(normalized))
      {
        matched.Add(normalized);
        matchCount++;
      }
    }
    if (matchCount == 0) return new LineClassification(LineKind.Plain, matched);
    if (matchCount == segments.Length) return new LineClassification(LineKind.Barrel, matched);

    // Borderline candidate. Promote to ExtendedBarrel if the non-keyword segments are all
    // parameterized variants and the line lacks ability-text structure.
    if (line.Length <= CompoundLineMaxLength && !SentenceStructurePattern().IsMatch(line))
    {
      bool allKeywordShaped = true;
      foreach (var seg in segments)
      {
        var trimmed = seg.Trim();
        if (trimmed.Length == 0) continue;
        var normalized = NormalizeKeywordSegment(seg);
        if (keywordSet.Contains(normalized)) continue;
        if (ParameterizedKeywordPattern().IsMatch(trimmed)) continue;
        allKeywordShaped = false;
        break;
      }
      if (allKeywordShaped)
      {
        return new LineClassification(LineKind.ExtendedBarrel, matched);
      }
    }
    return new LineClassification(LineKind.Borderline, matched);
  }

  /// <summary>
  /// Normalize a single line segment for keyword comparison: trim, drop trailing magnitude
  /// (e.g. "Ward {2}", "Annihilator 6"), lowercase. The normalization is intentionally lossy —
  /// magnitudes aren't part of the keyword identity for clustering.
  /// </summary>
  private static string NormalizeKeywordSegment(string segment)
  {
    var trimmed = segment.Trim();
    var stripped = MagnitudeSuffixPattern().Replace(trimmed, "");
    return stripped.ToLowerInvariant();
  }

  /// <summary>
  /// SHA-256 of <c>"{namespace}:{card_id}:{seq}:{text}"</c>, first 16 bytes as a GUID.
  /// Deterministic across runs. The namespace ("oracle" / "keyword") keeps natural and
  /// synthetic IDs disjoint; <c>seq</c> disambiguates split/transform cards with repeated
  /// oracle lines so they don't collide.
  /// </summary>
  private static Guid StableLineId(string ns, Guid cardId, int seq, string text)
  {
    var input = Encoding.UTF8.GetBytes($"{ns}:{cardId}:{seq}:{text}");
    Span<byte> hash = stackalloc byte[32];
    SHA256.HashData(input, hash);
    return new Guid(hash[..16]);
  }
}
