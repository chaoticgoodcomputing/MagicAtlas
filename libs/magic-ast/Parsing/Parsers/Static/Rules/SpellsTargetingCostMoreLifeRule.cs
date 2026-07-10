namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Spells your opponents cast that target this creature cost an additional N life
/// to cast." (Terror of the Peaks) — the life-currency sibling of
/// <see cref="SpellsTargetingCostMoreRule"/> (which handles the mana-currency
/// "cost {N} more to cast" phrasing of the same pre-Ward targeting-tax shape).
///
/// <para>
/// CR 601.2f (verbatim): "The player determines the total cost of the spell. Usually
/// this is just the mana cost. Some spells have additional or alternative costs. ...
/// The total cost is the mana cost or alternative cost (as determined in rule 601.2b),
/// plus all additional costs and cost increases, and minus all cost reductions." The
/// increase here is paid in life rather than mana (CR 119 — life totals), so it is
/// carried on <see cref="CostIncreaseEffect.LifeAmount"/> rather than
/// <see cref="CostIncreaseEffect.Amount"/>/<see cref="CostIncreaseEffect.ManaSymbols"/>
/// (both mana-only fields); <see cref="CostIncreaseEffect.Amount"/> is left a zero
/// literal, mirroring how the colored-only Ruby Leech shape zeroes it.
/// </para>
///
/// Anchored (^…$) to the exact "Spells your opponents cast that target
/// [this creature/name] cost an additional N life to cast" surface, mirroring
/// <see cref="SpellsTargetingCostMoreRule"/>'s anchoring — the "an additional … life"
/// phrasing is disjoint from that rule's "{N} more" mana phrasing, so the two never
/// collide on the same text.
/// </summary>
[StaticRule(Priority = 942)]
public sealed class SpellsTargetingCostMoreLifeRule : IStaticRule
{
  private static readonly Regex _spellsTargetingCostMoreLifePattern = new(
    @"^\s*Spells\s+your\s+opponents\s+cast\s+that\s+target\s+(?:this\s+\w+|[A-Z][A-Za-z\s,']+?)\s+cost\s+an\s+additional\s+(?<amount>\d+)\s+life\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _spellsTargetingCostMoreLifePattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new CostIncreaseEffect
        {
          Amount = LiteralQuantity.Of(0),
          LifeAmount = LiteralQuantity.Of(amount),
          TargetedObject = ObjectReference.Self(),
          CasterFilter = ControllerFilter.Opponent,
        }],
      },
    ];
  }
}
