namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Equip abilities you activate that target this creature cost {N} less to
/// activate." (Fervent Champion) — a static cost reduction scoped to Equip
/// activated abilities the controller activates that ALSO target the source
/// creature itself. Sibling of <see cref="EquipAbilityYouActivateCostReductionRule"/>
/// (Éowyn, Lady of Rohan's unqualified "Equip abilities you activate cost {N}
/// less to activate.", no target predicate) and
/// <see cref="AppliesToCostReductionRule"/> (Strong Back's "…that target
/// enchanted creature…", a different referent). Here the referent of "this
/// creature" is the source object itself (CR 109.5), so the predicate is a
/// <see cref="ObjectReferenceKind.Self"/> reference rather than
/// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>.
///
/// <para>
/// CR 118.7 (cost reduction): "What a player actually needs to do to pay a cost
/// may be changed or reduced by effects." (CR 118.9 governs ALTERNATIVE costs,
/// not reduction.) CR 702.6 (Equip): the activated ability being reduced is the
/// Equipment's Equip ability. Per ADR 0003 follow-up 1, the class of reduced
/// abilities is keyed on the surviving keyword identity via an
/// <see cref="ActivatedAbilityReference"/> with <see cref="KeywordAbility.Equip"/>
/// and a "you activate" <see cref="ControllerFilter.You"/>, plus a
/// <see cref="AbilityReference.TargetsObject"/> predicate for "that target this
/// creature".
/// </para>
///
/// <para>
/// Anchored (^…$) so it matches ONLY the standalone sentence, and requires the
/// literal "that target this creature" clause — it cannot fire on the bare
/// EquipAbilityYouActivateCostReductionRule surface (no target clause) nor on
/// AppliesToCostReductionRule's "…that target enchanted creature…" surface (a
/// distinct referent noun phrase).
/// </para>
/// </summary>
[StaticRule(Priority = 991)]
public sealed class EquipAbilityYouActivateTargetingSelfCostReductionRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Equip\s+abilities\s+you\s+activate\s+that\s+target\s+this\s+creature\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+activate\.?\s*$",
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
            TargetsObject = new ObjectReference { Kind = ObjectReferenceKind.Self },
          },
        }],
      },
    ];
  }
}
