using System.Text.RegularExpressions;
using MagicAST;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.Common;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Clustering;

/// <summary>
/// Lexical-template clustering + proximity-weighted yield ranking over unparsed
/// oracle lines. Produces the PRIMARY pick surface
/// (<see cref="TriageReport.TopYieldClusters"/>): each cluster is a buildable
/// family (one normalized template → one parser surface), annotated with its
/// proximity-weighted fractional yield and the dominant
/// <c>(Pattern, LastAttemptedRule)</c> "where it fails" hint.
/// </summary>
/// <remarks>
/// Tokenization is symbology-aware placeholder substitution (see
/// <see cref="Tokenize"/>); the win over <c>FallbackParser.InferFailurePattern</c>
/// is that the clustering is data-driven (long-tail templates surface even when
/// no named pattern fits) and the template axis splits coarse failure buckets
/// (e.g. "UnparsedTriggered") into distinct, individually-pickable families.
/// </remarks>
public static class YieldClusterAnalyzer
{
  private const int MaxExemplars = 5;

  /// <summary>
  /// Cluster all unparsed lines by normalized template, compute per-template
  /// proximity-weighted fractional yield and dominant diagnostic, and return the
  /// top-<paramref name="batchSize"/> clusters ranked by fused score (parse
  /// proximity × downstream combo value).
  /// </summary>
  /// <param name="cardComboValue">
  /// Per-card combo-blocking value keyed by card name (from InteractionTriage).
  /// Pass an empty map to disable the fusion — the ranking then reduces to the
  /// pre-fusion fractional-yield order.
  /// </param>
  public static IReadOnlyList<YieldClusterSummary> ComputeTopYieldClusters(
    IEnumerable<ParseRecord> records,
    int batchSize,
    IReadOnlySet<string> handParsedNames,
    IReadOnlyDictionary<string, CardComboValue>? cardComboValue = null
  )
  {
    cardComboValue ??= new Dictionary<string, CardComboValue>(StringComparer.Ordinal);
    // 1) Per-card unparsed-line list. Card is "unparsed" if it has any line
    //    with at least one diagnostic. Unit of clustering is the oracle line.
    var unparsedCards = new List<UnparsedCard>();
    foreach (var rec in records)
    {
      var lines = rec.Lines.Where(l => l.Patterns.Count > 0).ToList();
      if (lines.Count == 0)
        continue;
      unparsedCards.Add(
        new UnparsedCard(rec.ScryfallId, rec.CardName, rec.Input, lines, rec.SuspectedLossy)
      );
    }

    // 2) Tokenize each unparsed line to its template, carrying the line's
    //    diagnostics so each cluster can report its dominant (pattern, rule).
    var lineEntries = new List<LineEntry>();
    foreach (var card in unparsedCards)
    {
      foreach (var line in card.Lines)
      {
        var template = Tokenize(line.OracleLine, card.CardName);
        lineEntries.Add(new LineEntry(card, line.OracleLine, template, line.Diagnostics));
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

    // 5a) Per-template proximity-weighted fractional yield: each card with a
    //     line in template T contributes 1 / (distinct templates on that card).
    //     Single pass over cards distributing weight to their templates.
    var templateToFractionalYield = byTemplate.ToDictionary(b => b.TemplateId, _ => 0.0);
    foreach (var (_, templates) in cardToTemplates)
    {
      if (templates.Count == 0)
        continue;
      var weight = 1.0 / templates.Count;
      foreach (var t in templates)
        templateToFractionalYield[t] += weight;
    }

    // 5a′) Per-template fractional downstream combo value. Same 1/N attribution
    //      as fractional yield: each card carrying a line in template T donates
    //      (its blocked-combo count / mass) ÷ (distinct templates on the card)
    //      to T. A card blocking many popular combos, one template from parsing,
    //      lifts that template's fused score; a card whose combo value is split
    //      across several unparsed templates shares the credit rather than
    //      double-counting it. Cards absent from the value map (parse fine, or
    //      block no combo) contribute 0 — so with an empty map every template's
    //      combo value is 0 and the fused score collapses to fractional yield.
    var cardIdToName = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var card in unparsedCards)
      cardIdToName[card.ScryfallId] = card.CardName;

    var templateToComboCount = byTemplate.ToDictionary(b => b.TemplateId, _ => 0.0);
    var templateToComboMass = byTemplate.ToDictionary(b => b.TemplateId, _ => 0.0);
    foreach (var (cardId, templates) in cardToTemplates)
    {
      if (templates.Count == 0)
        continue;
      if (
        !cardIdToName.TryGetValue(cardId, out var name)
        || !cardComboValue.TryGetValue(name, out var value)
      )
        continue;
      var weight = 1.0 / templates.Count;
      foreach (var t in templates)
      {
        templateToComboCount[t] += value.BlockedComboCount * weight;
        templateToComboMass[t] += value.PopularityMass * weight;
      }
    }

    // Fused score: parse-proximity yield scaled by an interaction-value boost.
    // log10 keeps the wide popularity-mass range bounded and additive; the
    // (1 + boost) form means a zero-combo-value cluster keeps its raw yield.
    var templateToFusedScore = byTemplate.ToDictionary(
      b => b.TemplateId,
      b =>
        templateToFractionalYield.GetValueOrDefault(b.TemplateId)
        * (1.0 + Math.Log10(1.0 + templateToComboMass.GetValueOrDefault(b.TemplateId)))
    );

    // 5b) Per-template dominant (pattern, rule): the most common diagnostic key
    //     across the cluster's lines — the "where it fails" navigation hint.
    var templateToDominant = byTemplate.ToDictionary(
      b => b.TemplateId,
      b => ComputeDominantDiagnostic(b.Lines)
    );

    // 6) Rank clusters by FUSED score (primary): proximity-weighted fractional
    //    yield scaled by the popularity-mass of the combos the cluster unblocks.
    //    Fractional yield alone surfaces templates that are the last-or-near-last
    //    missing piece across many partially-complete cards; the fused score adds
    //    the downstream objective — a surface that flips cards AND unblocks
    //    popular combos outranks one that only flips cards. With an empty value
    //    map the boost is 0 and this reduces EXACTLY to the prior fractional-yield
    //    order (fractional yield is kept as the first tiebreak to make that
    //    reduction exact). Overlap is handled implicitly: a card split across N
    //    templates contributes 1/N to each axis, so co-occurring templates are
    //    discounted rather than both claiming the full card.
    var ranked = byTemplate
      .OrderByDescending(b => templateToFusedScore.GetValueOrDefault(b.TemplateId))
      .ThenByDescending(b => templateToFractionalYield.GetValueOrDefault(b.TemplateId))
      .ThenByDescending(b => templateToDirectYield.GetValueOrDefault(b.TemplateId))
      .ThenByDescending(b => b.Lines.Count)
      .Take(batchSize)
      .ToList();

    var summaries = new List<YieldClusterSummary>(ranked.Count);
    for (var i = 0; i < ranked.Count; i++)
    {
      var bucket = ranked[i];

      // Exemplar selection: prefer genuinely-clean cards. A lossy-but-clean card
      // (SuspectedLossy — a non-target line silently collapsed) is ranked LAST
      // even if its OtherUnparsedClusters is 0, because that count can't see the
      // silent drop; then fewest other unparsed templates, then shortest line.
      var exemplars = bucket.Lines
        .Select(l => new { Line = l, OtherClusters = cardToTemplates[l.Card.ScryfallId].Count - 1 })
        .GroupBy(x => x.Line.Card.ScryfallId)
        .Select(g => g.First())
        .OrderBy(x => x.Line.Card.SuspectedLossy)
        .ThenBy(x => x.OtherClusters)
        .ThenBy(x => x.Line.OracleLine.Length)
        .Take(MaxExemplars)
        .Select(x => new YieldExemplar
        {
          CardName = x.Line.Card.CardName,
          ScryfallId = x.Line.Card.ScryfallId,
          OracleLine = x.Line.OracleLine,
          OtherUnparsedClusters = x.OtherClusters,
          LossyRisk = x.Line.Card.SuspectedLossy,
          AlreadyHandParsed = handParsedNames.Contains(x.Line.Card.CardName),
          Input = x.Line.Card.Input,
        })
        .ToList();

      var (dominantPattern, dominantRule, dominantShare) = templateToDominant[bucket.TemplateId];

      var comboMass = templateToComboMass.GetValueOrDefault(bucket.TemplateId);
      var interactionValueScore = Math.Log10(1.0 + comboMass);

      summaries.Add(new YieldClusterSummary
      {
        Rank = i + 1,
        Template = bucket.Template,
        LineCount = bucket.Lines.Count,
        CardCount = templateToCardIds[bucket.TemplateId].Count,
        DirectYield = templateToDirectYield.GetValueOrDefault(bucket.TemplateId),
        FractionalYield = templateToFractionalYield.GetValueOrDefault(bucket.TemplateId),
        ComboBlockedCount = templateToComboCount.GetValueOrDefault(bucket.TemplateId),
        ComboPopularityMass = comboMass,
        InteractionValueScore = interactionValueScore,
        FusedScore = templateToFusedScore.GetValueOrDefault(bucket.TemplateId),
        DominantPattern = dominantPattern,
        DominantLastAttemptedRule = dominantRule,
        DominantShare = dominantShare,
        Exemplars = exemplars,
      });
    }

    return summaries;
  }

  // ──────────────────────────────────────────────────────────────────────
  // Tokenization — placeholder substitution rules
  // ──────────────────────────────────────────────────────────────────────

  private static readonly Regex SymbolRegex = new(@"\{[^}]*\}", RegexOptions.Compiled);

  // Power/toughness deltas as a UNIT, before number normalization, so that
  // "+1/+1", "+2/+2", "-1/-1", and "+X/+X" all collapse to one <PT> template
  // (they all map to the same ModifyPT parser surface). Must run before
  // NumberRegex, which would otherwise split "+1/+1" into "+<N>/+<N>".
  private static readonly Regex PowerToughnessRegex = new(
    @"[+\-](?:\d+|X|Y|Z)/[+\-](?:\d+|X|Y|Z)",
    RegexOptions.Compiled
  );

  // Internal sentinel marking a mana-cost symbol; consecutive sentinels collapse
  // to a single <COST> so "{3}{U}" and "{2}" share a template.
  private const string CostMarker = "\u0001";
  private static readonly Regex CostMarkerRunRegex = new(CostMarker + "+", RegexOptions.Compiled);

  // Non-mana action/marker symbols, derived from the canonical Scryfall symbol
  // table at tests/atlas-flow-test/Data/_01_Raw/Datasets/External/symbology.json
  // (the entries with represents_mana == false). Embedded rather than loaded at
  // runtime: the list is tiny and stable, and any symbol NOT in this set is
  // treated as a cost symbol — so a future mana symbol degrades gracefully to
  // <COST> instead of crashing. {T} and {E} get their own placeholders because
  // tap-activated and energy abilities are large, distinct parser surfaces;
  // the rest collapse to <SYM>.
  private static readonly IReadOnlyDictionary<string, string> NonManaSymbols =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["{T}"] = "<TAP>",
      ["{Q}"] = "<UNTAP>",
      ["{E}"] = "<ENERGY>",
      ["{PW}"] = "<SYM>",
      ["{CHAOS}"] = "<SYM>",
      ["{A}"] = "<SYM>",
      ["{TK}"] = "<SYM>",
      ["{P}"] = "<SYM>",
    };

  private static readonly Regex NumberWordRegex = new(
    @"\b(one|two|three|four|five|six|seven|eight|nine|ten)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
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
  /// card name, mana/action symbols, P/T deltas, types, colors, subtypes,
  /// keywords, triggers, and numbers with placeholder tokens. Exact-match
  /// equality on the result is the cluster key. Normalization is deliberately
  /// aggressive on numeric magnitude: <c>+1/+1</c> and <c>+2/+2</c> collapse to
  /// the same <c>&lt;PT&gt;</c>, and <c>{2}</c> / <c>{3}{U}</c> collapse to the
  /// same <c>&lt;COST&gt;</c>, because they map to a single parser surface even
  /// though the literal text differs (the clusterer's question is "which rule
  /// to build", not "what does the card do").
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

    // 2) P/T deltas as a unit, BEFORE number normalization (so "+1/+1" becomes
    //    one <PT>, not "+<N>/+<N>").
    text = PowerToughnessRegex.Replace(text, "<PT>");

    // 3) Symbols: mana-cost symbols → a sentinel (consecutive sentinels collapse
    //    to one <COST>, so "{3}{U}" and "{2}" share a template); the small set
    //    of non-mana action symbols → their own placeholders ({T} → <TAP>, etc.).
    text = SymbolRegex.Replace(
      text,
      m => NonManaSymbols.TryGetValue(m.Value, out var placeholder) ? placeholder : CostMarker
    );
    text = CostMarkerRunRegex.Replace(text, "<COST>");

    // 4) Placeholder substitutions, ordered most-specific to least.
    text = NonColorRegex.Replace(text, "<NON>");
    text = ColorWordRegex.Replace(text, "<COLOR>");
    text = CardTypeRegex.Replace(text, "<TYPE>");
    text = KeywordRegex.Replace(text, "<KW>");
    text = TriggerWordRegex.Replace(text, "<TRIG>");
    text = NumberWordRegex.Replace(text, "<N>");
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

  /// <summary>
  /// Most common <c>(Pattern, LastAttemptedRule)</c> across a cluster's lines —
  /// the cluster's "where it fails" hint. Ties broken deterministically by
  /// pattern then rule name so the report is stable across runs. Returns
  /// <c>("Unknown", null)</c> if the cluster carries no diagnostics (shouldn't
  /// happen — every clustered line is unparsed).
  /// </summary>
  private static (string Pattern, string? LastAttemptedRule, double Share) ComputeDominantDiagnostic(
    IReadOnlyList<LineEntry> lines
  )
  {
    var diagnostics = lines.SelectMany(l => l.Diagnostics).ToList();
    var best = diagnostics
      .GroupBy(d => (d.Pattern, d.LastAttemptedRule))
      .OrderByDescending(g => g.Count())
      .ThenBy(g => g.Key.Pattern, StringComparer.Ordinal)
      .ThenBy(g => g.Key.LastAttemptedRule, StringComparer.Ordinal)
      .FirstOrDefault();

    if (best is null)
      return ("Unknown", null, 1.0);

    // Diagnostic-spread homogeneity: the fraction of this cluster's failure signals that
    // are the single dominant (pattern, rule). A cluster that lumps lines bailing in
    // several different parsers despite a shared lexical template scores low — the
    // over-collapse heterogeneity exact-template clustering can't see (initiative 02).
    var share = (double)best.Count() / diagnostics.Count;
    return (best.Key.Pattern, best.Key.LastAttemptedRule, share);
  }

  // ──────────────────────────────────────────────────────────────────────
  // Internal scratch records
  // ──────────────────────────────────────────────────────────────────────

  private sealed record UnparsedCard(
    string ScryfallId,
    string CardName,
    CardInputDTO Input,
    IReadOnlyList<LineOutcome> Lines,
    bool SuspectedLossy
  );

  private sealed record LineEntry(
    UnparsedCard Card,
    string OracleLine,
    string Template,
    IReadOnlyList<LineDiagnostic> Diagnostics
  );

  private sealed record ClusterBucket(int TemplateId, string Template, IReadOnlyList<LineEntry> Lines);
}
