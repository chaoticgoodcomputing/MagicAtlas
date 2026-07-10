namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Unlock costs you pay cost {N} less." — a static cost reduction scoped to the
/// CLASS of costs the controller pays to unlock a locked half (door) of a Room
/// permanent (Inquisitive Glimmer). The sibling of
/// <see cref="EquipAbilityYouActivateCostReductionRule"/> for a different class of
/// cost — same reduction shape (<see cref="CostReductionEffect.AppliesTo"/>, a
/// class reference keyed on the surviving keyword identity), different verb
/// ("pay" rather than "activate") because unlocking a door is a special action
/// (CR 709.5e), not an activated ability put on the stack.
///
/// <para>
/// CR 118.7 (cost reduction): "What a player actually needs to do to pay a cost
/// may be changed or reduced by effects." (CR 118.9 governs ALTERNATIVE costs, not
/// reduction.) CR 709.5e: "A player who controls a permanent that has one or more
/// locked halves may pay the mana cost of a locked half of that permanent to give
/// that permanent the appropriate unlocked designation." CR 709.5f: "Some spells
/// and abilities instruct a player to 'unlock' half of a permanent." The reduced
/// class is keyed via <see cref="KeywordAbility.Unlock"/> on an
/// <see cref="ActivatedAbilityReference"/> with <see cref="ControllerFilter.You"/> —
/// the same reference shape <see cref="EquipAbilityYouActivateCostReductionRule"/>
/// builds for Equip, reused here for Unlock (ADR 0003 follow-up 1: the surviving
/// keyword identity is what a reference filter matches on).
/// </para>
///
/// <para>
/// Anchored (^…$) so it matches ONLY the standalone sentence, with no "to activate"
/// / "to cast" suffix (unlike the Equip/spell cost-reduction siblings) — the oracle
/// wording is "cost {N} less." with no trailing infinitive.
/// </para>
/// </summary>
[StaticRule(Priority = 991)]
public sealed class UnlockCostsYouPayCostReductionRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Unlock\s+costs\s+you\s+pay\s+cost\s+\{(?<amount>\d+)\}\s+less\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new CostReductionEffect
        {
          Amount = LiteralQuantity.Of(int.Parse(match.Groups["amount"].Value)),
          AppliesTo = new ActivatedAbilityReference
          {
            Keyword = KeywordAbility.Unlock,
            Controller = ControllerFilter.You,
          },
        }],
      },
    ];
  }
}
