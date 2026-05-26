using System.Text.RegularExpressions;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;

/// <summary>
/// Lexical-template clustering over unparsed oracle lines, plus greedy
/// set-cover yield projection. Produces <see cref="YieldClustersReport"/> as
/// a discovery-side companion to the pattern-driven <c>TriageReport</c>.
/// </summary>
/// <remarks>
/// V1 — exact-match template clustering only. No fuzzy merging. Tokenization
/// is hand-coded placeholder substitution; the win is that the placeholder
/// rules are simpler than the regex heuristics in
/// <c>FallbackParser.InferFailurePattern</c> AND the downstream clustering is
/// data-driven (long-tail templates surface even when no named pattern fits).
/// </remarks>
[FlowthruStep]
public static class EmitYieldClustersStep
{
  private const int MaxExemplars = 8;
  private const int MaxCoOccurring = 5;
  private const int BatchSize = 5;

  public static Func<IEnumerable<ParseRecord>, YieldClustersReport> Create() =>
    records =>
    {
      var all = records.ToList();

      // 1) Build per-card unparsed-line list. Card is "unparsed" if it has any
      //    line with at least one diagnostic. The unit of clustering is the
      //    oracle line, not the failure-pattern bucket.
      var unparsedCards = new List<UnparsedCard>();
      foreach (var rec in all)
      {
        var lines = rec.Lines.Where(l => l.Patterns.Count > 0).ToList();
        if (lines.Count == 0)
          continue;
        unparsedCards.Add(new UnparsedCard(rec.ScryfallId, rec.CardName, rec.Input.Name, lines));
      }

      // 2) Tokenize each unparsed line to its template. The card name is
      //    passed in so it can be substituted with <SELF> for the self-
      //    reference case (e.g., "Whenever Radha attacks").
      var lineEntries = new List<LineEntry>();
      foreach (var card in unparsedCards)
      {
        foreach (var line in card.Lines)
        {
          var template = Tokenize(line.OracleLine, card.CardName);
          lineEntries.Add(new LineEntry(card, line.OracleLine, template));
        }
      }

      var totalUnparsedCards = unparsedCards.Count;
      var totalUnparsedLines = lineEntries.Count;

      // 3) Group lines by template (exact-match).
      var byTemplate = lineEntries
        .GroupBy(e => e.Template)
        .Select((g, idx) => new { TemplateId = idx + 1, Template = g.Key, Lines = g.ToList() })
        .ToList();

      // 4) Build per-card cluster-set: {templates of this card's unparsed lines}.
      //    Cards whose ENTIRE unparsed set is one template are direct-yield
      //    candidates for that template.
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

      // 5) Per-template stats: direct yield (cards whose only template is this
      //    one), partial yield (cards with this template AND others).
      var templateToDirectYield = new Dictionary<int, int>();
      var templateToPartialYield = new Dictionary<int, int>();
      var templateToCardIds = new Dictionary<int, HashSet<string>>();
      foreach (var bucket in byTemplate)
      {
        templateToCardIds[bucket.TemplateId] = bucket.Lines.Select(l => l.Card.ScryfallId).ToHashSet();
      }

      foreach (var (cardId, templates) in cardToTemplates)
      {
        if (templates.Count == 1)
        {
          var t = templates.First();
          templateToDirectYield[t] = templateToDirectYield.GetValueOrDefault(t) + 1;
        }
        else
        {
          foreach (var t in templates)
          {
            templateToPartialYield[t] = templateToPartialYield.GetValueOrDefault(t) + 1;
          }
        }
      }

      // 6) Co-occurring clusters per template (Jaccard, top 5).
      var templateCoOccurrence = ComputeCoOccurrence(byTemplate, templateToCardIds);

      // 7) Exemplar selection per template: cards with the fewest OTHER
      //    unparsed templates first, then by oracle-line length ascending.
      var clusters = byTemplate
        .Select(bucket => new TemplateCluster
        {
          Id = bucket.TemplateId,
          Template = bucket.Template,
          LineCount = bucket.Lines.Count,
          CardCount = templateToCardIds[bucket.TemplateId].Count,
          DirectYield = templateToDirectYield.GetValueOrDefault(bucket.TemplateId),
          PartialYield = templateToPartialYield.GetValueOrDefault(bucket.TemplateId),
          ExemplarLines = bucket.Lines
            .Select(l => new
            {
              Line = l,
              OtherClusters = cardToTemplates[l.Card.ScryfallId].Count - 1,
            })
            .OrderBy(x => x.OtherClusters)
            .ThenBy(x => x.Line.OracleLine.Length)
            // Distinct cards in exemplars — don't show 5 copies of "Counter target spell"
            .GroupBy(x => x.Line.Card.ScryfallId)
            .Select(g => g.First())
            .Take(MaxExemplars)
            .Select(x => new ExemplarLine
            {
              CardName = x.Line.Card.CardName,
              ScryfallId = x.Line.Card.ScryfallId,
              OracleLine = x.Line.OracleLine,
              OtherUnparsedClusters = x.OtherClusters,
            })
            .ToList(),
          CoOccurringClusters = templateCoOccurrence.GetValueOrDefault(bucket.TemplateId, new List<int>()),
        })
        .OrderByDescending(c => c.DirectYield)
        .ThenByDescending(c => c.CardCount)
        .ToList();

      // 8) Greedy set-cover for K=BatchSize.
      var recommended = GreedyBatch(cardToTemplates, clusters, BatchSize);

      return new YieldClustersReport
      {
        GeneratedAt = DateTime.UtcNow.ToString("O"),
        TotalUnparsedCards = totalUnparsedCards,
        TotalUnparsedLines = totalUnparsedLines,
        DistinctTemplates = byTemplate.Count,
        Clusters = clusters,
        RecommendedBatch = recommended,
      };
    };

  // ──────────────────────────────────────────────────────────────────────
  // Tokenization
  // ──────────────────────────────────────────────────────────────────────

  // Order matters: process longest/most-specific patterns first to avoid
  // partial replacements (e.g., mana symbols before numbers).
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
  // Common keyword grants/triggers
  private static readonly Regex KeywordRegex = new(
    @"\b(flying|vigilance|trample|haste|first strike|double strike|reach|menace|deathtouch|lifelink|hexproof|shroud|defender|indestructible|protection|ward|flash|prowess|scry|surveil|investigate|monstrosity|level up|cycling|kicker|flashback|madness|delve|convoke|cascade|storm|threshold|morbid|fateful hour|raid|landfall|metalcraft|hellbent|ferocious|prowl|persist|undying)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  // Trigger timing words
  private static readonly Regex TriggerWordRegex = new(
    @"\b(when|whenever|at)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  private static string Tokenize(string oracleLine, string cardName)
  {
    var text = oracleLine.Trim();

    // 1) Card name → <SELF> (only if the line references the card by name).
    if (!string.IsNullOrWhiteSpace(cardName))
    {
      // First word of card name (handles "Radha, Heir to Keld" → match "Radha").
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

    // 3) Capitalized words remaining are likely subtypes (Goblin, Dragon, etc.)
    //    or proper nouns we haven't normalized. Replace with <SUBTYPE> if not
    //    sentence-initial. Skip if it's already a placeholder.
    text = Regex.Replace(text,
      @"(?<![A-Z<])\b[A-Z][a-z]{2,}\b",
      "<SUBTYPE>");

    // 4) Collapse repeated placeholders ("<SUBTYPE> <SUBTYPE>" → "<SUBTYPE>")
    //    and trim whitespace.
    text = Regex.Replace(text, @"(<SUBTYPE>\s+){2,}<SUBTYPE>", "<SUBTYPE>");
    text = Regex.Replace(text, @"\s+", " ").Trim();

    return text;
  }

  // ──────────────────────────────────────────────────────────────────────
  // Yield projection — greedy set-cover
  // ──────────────────────────────────────────────────────────────────────

  private static IReadOnlyList<BatchRecommendation> GreedyBatch(
    Dictionary<string, HashSet<int>> cardToTemplates,
    IReadOnlyList<TemplateCluster> clusters,
    int k
  )
  {
    // Working set: remaining cards keyed by ScryfallId → remaining uncovered template IDs.
    var remaining = cardToTemplates.ToDictionary(kv => kv.Key, kv => new HashSet<int>(kv.Value));
    var picked = new HashSet<int>();
    var recs = new List<BatchRecommendation>();
    var cumulative = 0;

    for (var rank = 1; rank <= k; rank++)
    {
      // For each unpicked cluster T, count cards whose remaining set is exactly {T}
      // — that's the marginal yield if we add T to the batch.
      var best = (clusterId: -1, marginal: 0);
      foreach (var c in clusters)
      {
        if (picked.Contains(c.Id))
          continue;
        var marginal = remaining.Count(kv => kv.Value.Count == 1 && kv.Value.Contains(c.Id));
        if (marginal > best.marginal)
          best = (c.Id, marginal);
      }

      if (best.clusterId == -1)
        break;

      picked.Add(best.clusterId);
      cumulative += best.marginal;

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

      var cluster = clusters.First(c => c.Id == best.clusterId);
      recs.Add(new BatchRecommendation
      {
        Rank = rank,
        ClusterId = best.clusterId,
        Template = cluster.Template,
        MarginalYield = best.marginal,
        CumulativeYield = cumulative,
      });
    }

    return recs;
  }

  // ──────────────────────────────────────────────────────────────────────
  // Co-occurrence (Jaccard)
  // ──────────────────────────────────────────────────────────────────────

  private static Dictionary<int, IReadOnlyList<int>> ComputeCoOccurrence(
    IReadOnlyList<dynamic> byTemplate,
    Dictionary<int, HashSet<string>> templateToCardIds
  )
  {
    var result = new Dictionary<int, IReadOnlyList<int>>();
    foreach (var bucket in byTemplate)
    {
      int id = bucket.TemplateId;
      var myCards = templateToCardIds[id];
      if (myCards.Count == 0)
      {
        result[id] = new List<int>();
        continue;
      }
      var scored = new List<(int otherId, double jaccard)>();
      foreach (var other in byTemplate)
      {
        int otherId = other.TemplateId;
        if (otherId == id)
          continue;
        var otherCards = templateToCardIds[otherId];
        var inter = myCards.Intersect(otherCards).Count();
        if (inter == 0)
          continue;
        var union = myCards.Union(otherCards).Count();
        scored.Add((otherId, (double)inter / union));
      }
      result[id] = scored
        .OrderByDescending(x => x.jaccard)
        .Take(MaxCoOccurring)
        .Select(x => x.otherId)
        .ToList();
    }
    return result;
  }

  // ──────────────────────────────────────────────────────────────────────
  // Internal scratch records
  // ──────────────────────────────────────────────────────────────────────

  private sealed record UnparsedCard(
    string ScryfallId,
    string CardName,
    string InputName,
    IReadOnlyList<LineOutcome> Lines
  );

  private sealed record LineEntry(UnparsedCard Card, string OracleLine, string Template);
}
