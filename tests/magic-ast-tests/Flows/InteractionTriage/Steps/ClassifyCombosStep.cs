using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// Classifies each Commander Spellbook combo by its first blocking layer and ranks the work by
/// popularity — the interaction-triage analogue of <c>AggregateTriageReportStep</c>. Joins the lean
/// combos against <c>MagicAstTriage</c>'s parse records (on card name): a combo whose every card
/// fully parses is <b>parse-ready</b> (a candidate for the interaction loop); one with an unparsed
/// card is <b>parse-blocked</b> (routes to the mast-tdd-loop, with the blocking cards named). Emits
/// the ranked queues plus the popularity-weighted card-gap overlay.
/// </summary>
[FlowthruStep]
public static class ClassifyCombosStep
{
  private const int TopN = 50;

  public static Func<
    (IEnumerable<Combo> Combos, IEnumerable<ParseRecord> Records),
    InteractionTriageReport
  > Create() =>
    inputs =>
    {
      var combos = inputs.Combos.ToList();
      var records = inputs.Records.ToList();

      var inCorpus = records.Select(r => r.CardName).ToHashSet(StringComparer.Ordinal);
      var fullyParsed = records
        .Where(r => r.TotalAbilities > 0 && r.TotalAbilities == r.ParsedAbilities)
        .Select(r => r.CardName)
        .ToHashSet(StringComparer.Ordinal);

      var classified = combos
        .Select(c =>
        (
          Combo: c,
          Blocking: c.Cards.Where(card => !fullyParsed.Contains(card.Name))
            .Select(card => card.Name)
            .ToList()
        ))
        .ToList();

      var parseReady = classified.Where(x => x.Blocking.Count == 0).ToList();
      var parseBlocked = classified.Where(x => x.Blocking.Count > 0).ToList();

      static ComboWorkItem ToItem((Combo Combo, List<string> Blocking) x) =>
        new()
        {
          ComboId = x.Combo.Id,
          Popularity = x.Combo.Popularity,
          Cards = x.Combo.Cards.Select(c => c.Name).ToList(),
          Results = x.Combo.Results,
          BlockingCards = x.Blocking,
        };

      // Full per-card blocking overlay, ranked by the downstream value a card
      // gates. PopularityMass (sum of blocked-combo popularity) is the primary
      // key — it rewards a card that blocks MANY combos, not just one popular
      // one — and is the weight the MagicAstTriage flow fuses into its yield
      // clusters. Emitted untruncated as AllComboBlockingCards; TopComboBlockingCards
      // is the human-facing top slice.
      var allCardGaps = parseBlocked
        .SelectMany(x => x.Blocking.Select(card => (Card: card, x.Combo.Popularity)))
        .GroupBy(t => t.Card, StringComparer.Ordinal)
        .Select(g => new CardGap
        {
          Card = g.Key,
          Reason = inCorpus.Contains(g.Key) ? "unparsed" : "missing-from-corpus",
          BlockedComboCount = g.Count(),
          MaxComboPopularity = g.Max(t => t.Popularity),
          PopularityMass = g.Sum(t => (long)t.Popularity),
        })
        .OrderByDescending(cg => cg.PopularityMass)
        .ThenByDescending(cg => cg.BlockedComboCount)
        .ToList();

      return new InteractionTriageReport
      {
        GeneratedAt = DateTime.UtcNow,
        TotalCombos = combos.Count,
        ParseReady = parseReady.Count,
        ParseBlocked = parseBlocked.Count,
        TopReconstructionCandidates = parseReady
          .OrderByDescending(x => x.Combo.Popularity)
          .Take(TopN)
          .Select(ToItem)
          .ToList(),
        TopParseBlocked = parseBlocked
          .OrderByDescending(x => x.Combo.Popularity)
          .Take(TopN)
          .Select(ToItem)
          .ToList(),
        TopComboBlockingCards = allCardGaps.Take(TopN).ToList(),
        AllComboBlockingCards = allCardGaps,
      };
    };
}
