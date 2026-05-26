using System.Text.RegularExpressions;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Clustering;

/// <summary>
/// Lexical-template clustering + greedy set-cover yield projection over
/// unparsed oracle lines. Shared between the in-line top-K summary embedded
/// in <see cref="TriageReport.TopYieldClusters"/> and any future full-detail
/// emit path.
/// </summary>
/// <remarks>
/// V1 — exact-match template clustering only. No fuzzy merging. Tokenization
/// is hand-coded placeholder substitution; the win is that the placeholder
/// rules are simpler than the regex heuristics in
/// <c>FallbackParser.InferFailurePattern</c> AND the downstream clustering is
/// data-driven (long-tail templates surface even when no named pattern fits).
/// </remarks>
public static class YieldClusterAnalyzer
{
  private const int MaxExemplars = 5;

  /// <summary>
  /// Cluster all unparsed lines, project yields, run greedy set-cover for
  /// <paramref name="batchSize"/> picks, and return the top-K cluster
  /// summaries in greedy-pick order.
  /// </summary>
  public static IReadOnlyList<YieldClusterSummary> ComputeTopYieldClusters(
    IEnumerable<ParseRecord> records,
    int batchSize
  )
  {
    // 1) Per-card unparsed-line list. Card is "unparsed" if it has any line
    //    with at least one diagnostic. Unit of clustering is the oracle line.
    var unparsedCards = new List<UnparsedCard>();
    foreach (var rec in records)
    {
      var lines = rec.Lines.Where(l => l.Patterns.Count > 0).ToList();
      if (lines.Count == 0)
        continue;
      unparsedCards.Add(new UnparsedCard(rec.ScryfallId, rec.CardName, lines));
    }

    // 2) Tokenize each unparsed line to its template.
    var lineEntries = new List<LineEntry>();
    foreach (var card in unparsedCards)
    {
      foreach (var line in card.Lines)
      {
        var template = Tokenize(line.OracleLine, card.CardName);
        lineEntries.Add(new LineEntry(card, line.OracleLine, template));
      }
    }

    if (lineEntries.Count == 0)
      return Array.Empty<YieldClusterSummary>();

    // 3) Group lines by template (exact-match).
    var byTemplate = lineEntries
      .GroupBy(e => e.Template)
      .Select((g, idx) => new ClusterBucket(idx + 1, g.Key, g.ToList()))
      .ToList();

    // 4) Per-card cluster-set.
    var cardToTemplates = new Dictionary<string, HashSet<int>>();
    foreach (var bucket in byTemplate)
    {
      foreach (var le in bucket.Lines)
      {
        if (!cardToTemplates.TryGetValue(le.Card.ScryfallId, out var set))
          cardToTemplates[le.Card.ScryfallId] = set = new HashSet<int>();
        set.Add(bucket.TemplateId);
      }
    }

    // 5) Per-template direct yield (cards whose only template is this one).
    var templateToDirectYield = new Dictionary<int, int>();
    var templateToCardIds = new Dictionary<int, HashSet<string>>();
    foreach (var bucket in byTemplate)
    {
      templateToCardIds[bucket.TemplateId] = bucket.Lines.Select(l => l.Card.ScryfallId).ToHashSet();
    }

    foreach (var (_, templates) in cardToTemplates)
    {
      if (templates.Count == 1)
      {
        var t = templates.First();
        templateToDirectYield[t] = templateToDirectYield.GetValueOrDefault(t) + 1;
      }
    }

    // 6) Greedy set-cover for K=batchSize. Picks clusters in marginal-yield
    //    order, recomputing remaining uncovered cards after each pick.
    var remaining = cardToTemplates.ToDictionary(kv => kv.Key, kv => new HashSet<int>(kv.Value));
    var picked = new HashSet<int>();
    var summaries = new List<YieldClusterSummary>();
    var cumulative = 0;

    for (var rank = 1; rank <= batchSize; rank++)
    {
      var best = (clusterId: -1, marginal: 0);
      foreach (var c in byTemplate)
      {
        if (picked.Contains(c.TemplateId))
          continue;
        var marginal = remaining.Count(kv => kv.Value.Count == 1 && kv.Value.Contains(c.TemplateId));
        if (marginal > best.marginal)
          best = (c.TemplateId, marginal);
      }

      if (best.clusterId == -1)
        break;

      picked.Add(best.clusterId);
      cumulative += best.marginal;

      var bucket = byTemplate.First(c => c.TemplateId == best.clusterId);

      // Exemplar selection: distinct cards with fewest other unparsed
      // templates, then shortest oracle line.
      var exemplars = bucket.Lines
        .Select(l => new { Line = l, OtherClusters = cardToTemplates[l.Card.ScryfallId].Count - 1 })
        .GroupBy(x => x.Line.Card.ScryfallId)
        .Select(g => g.First())
        .OrderBy(x => x.OtherClusters)
        .ThenBy(x => x.Line.OracleLine.Length)
        .Take(MaxExemplars)
        .Select(x => new YieldExemplar
        {
          CardName = x.Line.Card.CardName,
          ScryfallId = x.Line.Card.ScryfallId,
          OracleLine = x.Line.OracleLine,
          OtherUnparsedClusters = x.OtherClusters,
        })
        .ToList();

      summaries.Add(new YieldClusterSummary
      {
        Rank = rank,
        Template = bucket.Template,
        LineCount = bucket.Lines.Count,
        CardCount = templateToCardIds[bucket.TemplateId].Count,
        DirectYield = templateToDirectYield.GetValueOrDefault(bucket.TemplateId),
        MarginalYield = best.marginal,
        CumulativeYield = cumulative,
        Exemplars = exemplars,
      });

      // Remove flipped cards from remaining; for cards still red, drop the
      // picked cluster from their uncovered set.
      var flippedIds = remaining
        .Where(kv => kv.Value.Count == 1 && kv.Value.Contains(best.clusterId))
        .Select(kv => kv.Key)
        .ToList();
      foreach (var id in flippedIds)
        remaining.Remove(id);
      foreach (var kv in remaining)
        kv.Value.Remove(best.clusterId);
    }

    return summaries;
  }

  // ──────────────────────────────────────────────────────────────────────
  // Tokenization — placeholder substitution rules
  // ──────────────────────────────────────────────────────────────────────

  private static readonly Regex ManaSymbolRegex = new(@"\{[^}]*\}", RegexOptions.Compiled);
  private static readonly Regex NumberRegex = new(@"\b\d+\b", RegexOptions.Compiled);
  private static readonly Regex CardTypeRegex = new(
    @"\b(creatures?|lands?|artifacts?|enchantments?|planeswalkers?|instants?|sorceries|sorcery|permanents?|spells?|tokens?|cards?)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex ColorWordRegex = new(
    @"\b(white|blue|black|red|green|colorless|multicolored|monocolored)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex NonColorRegex = new(
    @"\bnon(white|blue|black|red|green|basic|land|creature|artifact|token)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex KeywordRegex = new(
    @"\b(flying|vigilance|trample|haste|first strike|double strike|reach|menace|deathtouch|lifelink|hexproof|shroud|defender|indestructible|protection|ward|flash|prowess|scry|surveil|investigate|monstrosity|level up|cycling|kicker|flashback|madness|delve|convoke|cascade|storm|threshold|morbid|fateful hour|raid|landfall|metalcraft|hellbent|ferocious|prowl|persist|undying)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex TriggerWordRegex = new(
    @"\b(when|whenever|at)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <summary>
  /// Normalize an oracle line into a comparable template by substituting
  /// card name, mana costs, types, colors, subtypes, keywords, triggers, and
  /// numbers with placeholder tokens. Exact-match equality on the result is
  /// the cluster key.
  /// </summary>
  public static string Tokenize(string oracleLine, string cardName)
  {
    var text = oracleLine.Trim();

    // 1) Card name → <SELF> (only if the line references the card by name).
    if (!string.IsNullOrWhiteSpace(cardName))
    {
      var firstWord = cardName.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault();
      if (!string.IsNullOrEmpty(firstWord) && firstWord.Length >= 3)
      {
        text = Regex.Replace(text, $@"\b{Regex.Escape(firstWord)}\b", "<SELF>",
          RegexOptions.IgnoreCase);
      }
    }

    // 2) Placeholder substitutions, ordered most-specific to least.
    text = ManaSymbolRegex.Replace(text, "<COST>");
    text = NonColorRegex.Replace(text, "<NON>");
    text = ColorWordRegex.Replace(text, "<COLOR>");
    text = CardTypeRegex.Replace(text, "<TYPE>");
    text = KeywordRegex.Replace(text, "<KW>");
    text = TriggerWordRegex.Replace(text, "<TRIG>");
    text = NumberRegex.Replace(text, "<N>");

    // 3) Capitalized words remaining are likely subtypes or proper nouns
    //    not yet normalized. Replace with <SUBTYPE> if not following a
    //    placeholder boundary.
    text = Regex.Replace(text, @"(?<![A-Z<])\b[A-Z][a-z]{2,}\b", "<SUBTYPE>");

    // 4) Collapse repeated placeholders and whitespace.
    text = Regex.Replace(text, @"(<SUBTYPE>\s+){2,}<SUBTYPE>", "<SUBTYPE>");
    text = Regex.Replace(text, @"\s+", " ").Trim();

    return text;
  }

  // ──────────────────────────────────────────────────────────────────────
  // Internal scratch records
  // ──────────────────────────────────────────────────────────────────────

  private sealed record UnparsedCard(string ScryfallId, string CardName, IReadOnlyList<LineOutcome> Lines);

  private sealed record LineEntry(UnparsedCard Card, string OracleLine, string Template);

  private sealed record ClusterBucket(int TemplateId, string Template, IReadOnlyList<LineEntry> Lines);
}
