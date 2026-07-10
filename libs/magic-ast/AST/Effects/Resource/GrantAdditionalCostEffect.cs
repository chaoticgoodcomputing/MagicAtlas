namespace MagicAST.AST.Effects.Resource;

using MagicAST.AST.Costs;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A static ability that grants the controller the option to pay an additional
/// cost for a filtered class of spells they cast — "As an additional cost to
/// cast [filter] spells, you may pay [cost]." Additional-cost sibling of
/// <see cref="GrantAlternativeCostEffect"/> (which grants a SUBSTITUTE cost);
/// this grants a cost paid ON TOP OF the spell's normal cost.
///
/// <para>
/// CR 601.2f (verbatim): "The player determines the total cost of the spell.
/// Usually this is just the mana cost. Some spells have additional or
/// alternative costs. ... The total cost is the mana cost or alternative cost
/// (as determined in rule 601.2b), plus all additional costs and cost
/// increases, and minus all cost reductions."
/// </para>
///
/// <para>
/// Defiler of Instinct is the canonical example: "As an additional cost to
/// cast red permanent spells, you may pay 2 life." Unlike
/// <see cref="MagicAST.AST.Effects.CardFlow.AdditionalCastCostEffect"/> (a
/// CR-static keyword's own additional cost paid when casting THIS spell), this
/// effect permanently grants an additional-cost option for a filtered class of
/// OTHER spells the controller casts. The <see cref="AffectedSpells"/> filter
/// sits on this effect rather than on the enclosing
/// <see cref="MagicAST.AST.Abilities.StaticAbility.AffectedObjects"/>,
/// mirroring <see cref="GrantAlternativeCostEffect.AffectedSpells"/>: the
/// grantor (Defiler of Instinct) and the affected spells (red permanent
/// spells) are distinct objects.
/// </para>
/// </summary>
[OracleEffect("grantAdditionalCost")]
public sealed record GrantAdditionalCostEffect : Effect
{
  /// <summary>
  /// The additional cost the controller may pay when casting an affected
  /// spell, carrying its own <see cref="AdditionalCost.IsOptional"/> ("you may
  /// pay") flag. Typically a <see cref="PayLifeCost"/> (e.g. 2 life for
  /// Defiler of Instinct); the polymorphic <see cref="Cost"/> base
  /// accommodates other additional-cost shapes.
  /// </summary>
  public required AdditionalCost AdditionalCost { get; init; }

  /// <summary>
  /// The class of spells to which the additional-cost permission applies —
  /// the filter over spells the controller may cast paying this additional
  /// cost. e.g. Defiler of Instinct: <c>{ CardTypes: ["spell","permanent"],
  /// Colors: ["R"], Controller: You }</c>.
  /// </summary>
  public required ObjectFilter AffectedSpells { get; init; }
}
