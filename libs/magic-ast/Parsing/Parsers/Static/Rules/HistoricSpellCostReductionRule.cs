namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "Historic spells you cast cost {N} less to cast." — the static cost-reduction
/// ability granted by cards like Jhoira's Familiar (DOM).
///
/// CR 700.6: "The term historic refers to an object that has the legendary supertype,
/// the artifact card type, or the Saga subtype." Historic is a named game quality (not
/// a printed card type or supertype), so the filter uses <see cref="ObjectFilter.IsHistoric"/>
/// rather than the Supertypes/CardTypes axes.
///
/// Priority 985 — fires before <see cref="TypeSpellCostReductionRule"/> (priority 984)
/// so the "Historic" token is claimed before the generic single-noun type rule misclassifies
/// it as a creature subtype. The pattern is anchored (^…$) after stripping reminder text;
/// the "Historic" match is case-sensitive (oracle text always uses title case for the quality
/// name) to prevent false positives.
/// </summary>
[StaticRule(Priority = 985)]
public sealed class HistoricSpellCostReductionRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Strip reminder text before matching (CR 207.2 — reminder text is parenthetical
    // and not part of the ability's rules text).
    var stripped = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _historicSpellCostReductionPattern.Match(stripped);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Resource.CostReductionEffect
          {
            Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
          },
        ],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          IsHistoric = true,
          Controller = ControllerFilter.You,
        },
      },
    ];
  }

  // Anchored pattern: "Historic spells you cast cost {N} less to cast."
  // Reminder text "(Artifacts, legendaries, and Sagas are historic.)" is stripped
  // before matching by StripReminderText, so the pattern does not need to accommodate it.
  // Amount is a single generic-mana digit (the only printed form for this ability family).
  private static readonly Regex _historicSpellCostReductionPattern = new(
    @"^\s*Historic\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );
}
