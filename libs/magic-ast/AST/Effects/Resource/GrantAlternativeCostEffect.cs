namespace MagicAST.AST.Effects.Resource;

using MagicAST.AST.Costs;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A static ability that grants the controller the option to pay an alternative
/// cost rather than the normal mana cost for a filtered class of spells they cast.
///
/// <para>
/// CR 118.9 (verbatim): "Some spells have alternative costs. An alternative cost is a
/// cost listed in a spell's text, or applied to it from another effect, that its
/// controller may pay rather than paying the spell's mana cost. Alternative costs are
/// usually phrased, 'You may [action] rather than pay [this object's] mana cost,' or
/// 'You may cast [this object] without paying its mana cost.'"
/// </para>
///
/// <para>
/// Rooftop Storm (ISD) is the canonical example: "You may pay {0} rather than pay
/// the mana cost for Zombie creature spells you cast." Unlike
/// <see cref="AlternativeCastEffect"/> (which changes the zone a card is cast
/// <em>from</em>), this effect permanently grants a cost-substitution option for a
/// filtered class of OTHER spells. Unlike <see cref="AlternativePaymentEffect"/>
/// (Convoke/Delve — payment substitutions mid-payment), this replaces the entire mana
/// cost before payment begins (CR 601.2b). The <see cref="AffectedSpells"/> filter
/// sits on this effect rather than on the enclosing
/// <see cref="MagicAST.AST.Abilities.StaticAbility.AffectedObjects"/>; the
/// grantor (Rooftop Storm) and the affected spells (Zombie creatures) are distinct
/// objects.
/// </para>
/// </summary>
[OracleEffect("grantAlternativeCost")]
public sealed record GrantAlternativeCostEffect : Effect
{
  /// <summary>
  /// The alternative cost the controller may pay instead of the spell's mana cost.
  /// Typically a <see cref="ManaCost"/> (e.g. <c>{0}</c> for Rooftop Storm); the
  /// polymorphic <see cref="Cost"/> base accommodates non-mana alternatives.
  /// </summary>
  public required Cost AlternativeCost { get; init; }

  /// <summary>
  /// The class of spells to which the alternative-cost permission applies — the
  /// filter over spells the controller may cast for the alternative cost.
  /// e.g. Rooftop Storm: <c>{ CardTypes: ["spell","creature"], Subtypes: ["Zombie"],
  /// Controller: You }</c>.
  /// </summary>
  public required ObjectFilter AffectedSpells { get; init; }
}
