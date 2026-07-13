namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "The first creature spell you cast each turn costs {2} less to cast." (Shadow
/// in the Warp) — CR 601.2f cost-reduction static ability, scoped to only the
/// FIRST spell of the named type the controller casts in a given turn. Sibling
/// of <see cref="TypeSpellCostReductionRule"/> (same <c>CostReductionEffect</c> +
/// <see cref="MagicAST.AST.References.ObjectFilter"/> shape rooted at
/// <c>CardTypes: ["spell", &lt;type&gt;]</c> via
/// <see cref="StaticRuleHelpers.BuildTypeSpellFilter(string)"/>), but additionally
/// narrowed by an ordinal "first ... each turn" qualifier — the same descriptive
/// occurrence-counting convention <see cref="Triggered.Rules.DrawNthCardEachTurnConditionRule"/>
/// uses on <c>TriggerCondition.Ordinal</c>/<c>PerTurn</c>, here carried on
/// <see cref="MagicAST.AST.References.CastThisTurnPredicate.Ordinal"/> (a backward-
/// looking <see cref="MagicAST.AST.References.HistoryPredicate"/> restricting the
/// filter, CR 601.2f: "the effect that applies while the spell is on the stack").
/// MAST records which occurrence the oracle text names ("first"); the per-turn
/// tally and resetting are engine territory, not modelled here.
///
/// <para>
/// Priority 1002 — above every other cost-reduction rule (max prior priority
/// 1001) so the "The first ..." anchor is tried before any generic type/spell
/// cost-reduction shape could otherwise be reached; the anchored <c>^…$</c>
/// pattern with the literal "The first" lead-in and "each turn" tail means it
/// cannot collide with any sibling that lacks both qualifiers.
/// </para>
/// </summary>
[StaticRule(Priority = 1002)]
public sealed class FirstTypeSpellEachTurnCostReductionRule : IStaticRule
{
  // "The first <ordinal-selectable type noun> spell you cast each turn costs {N}
  // less to cast." — anchored end-to-end so it only claims the exact ordinal-
  // qualified shape and never a substring of a longer/differently-shaped clause.
  private static readonly Regex _pattern = new(
    @"^\s*The\s+first\s+(?<filter>[A-Za-z]+)\s+spell\s+you\s+cast\s+each\s+turn\s+costs\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var amount = int.Parse(match.Groups["amount"].Value);

    var affected = StaticRuleHelpers.BuildTypeSpellFilter(filterText);
    if (affected is null)
    {
      return null;
    }

    affected = affected with
    {
      History = new CastThisTurnPredicate { Caster = ControllerFilter.You, Ordinal = 1 },
    };

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        }],
        AffectedObjects = affected,
      },
    ];
  }
}
