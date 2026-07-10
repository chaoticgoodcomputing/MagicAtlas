namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Equip abilities you activate cost {N} less to activate." — a static cost
/// reduction scoped to the CLASS of Equip activated abilities the controller
/// activates (Éowyn, Lady of Rohan; Arms Scavenger; Bureau Headmaster; Fighter
/// Class's level-2 bar). Unlike Strong Back's
/// "Equip abilities you activate <b>that target enchanted creature</b> cost {3}
/// less to activate." (handled by <see cref="AppliesToCostReductionRule"/>), the
/// reference here carries NO target predicate — it applies to every Equip ability
/// the controller activates (the LeoninShikari-style unqualified
/// <see cref="ActivatedAbilityReference"/>).
///
/// <para>
/// CR 118.9 (cost alterations): "Some effects reduce the cost to cast a spell or
/// activate an ability." CR 702.6 (Equip): the activated ability being reduced is
/// the Equipment's Equip ability. The class of reduced abilities is keyed on the
/// surviving keyword identity (ADR 0003 follow-up 1) via an
/// <see cref="ActivatedAbilityReference"/> with <see cref="KeywordAbility.Equip"/>
/// and a "you activate" <see cref="ControllerFilter.You"/> — the exact same
/// reference shape <see cref="AppliesToCostReductionRule"/> builds, minus the
/// <c>TargetsObject</c> predicate.
/// </para>
///
/// <para>
/// Anchored (^…$) so it matches ONLY the standalone sentence: it cannot fire on
/// Nahiri, Storm of Stone's compound line ("During your turn, creatures you
/// control have first strike and equip abilities you activate cost {1} less to
/// activate.") — that carries a different prefix and is not a bare sentence — and
/// it cannot fire on Strong Back's "…that target enchanted creature…" variant,
/// whose extra clause sits between "you activate" and "cost".
/// </para>
/// </summary>
[StaticRule(Priority = 991)]
public sealed class EquipAbilityYouActivateCostReductionRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Equip\s+abilities\s+you\s+activate\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+activate\.?\s*$",
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
            Keyword = KeywordAbility.Equip,
            Controller = ControllerFilter.You,
          },
        }],
      },
    ];
  }
}
