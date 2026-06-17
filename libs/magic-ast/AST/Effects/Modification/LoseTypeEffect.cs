namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[This permanent] isn't a [card type]" / "[target] loses [card type] type" —
/// a layer-4 continuous effect (CR 613.1d) that removes a named card type from
/// the subject. The canonical Theros God template: "As long as your devotion to
/// [color] is less than [N], [this card name] isn't a creature." (CR 205.1a:
/// when an effect says a permanent "isn't" of a given type, it loses that type
/// for the duration of the effect.)
///
/// <para>
/// This is the structural inverse of <see cref="BecomesCreatureEffect"/>: where
/// that node ADDS a type (often with a full characteristic bundle), this node
/// REMOVES a single named type from the permanent's type line. The removed type
/// is a card-type keyword (creature, artifact, enchantment, land, planeswalker,
/// battle — CR 205.2), not a subtype (CR 205.3) or supertype (CR 205.4); those
/// are handled by <see cref="ChangeSubtypeEffect"/> and future nodes respectively.
/// </para>
///
/// <para>
/// Distinct from <see cref="LoseAbilityEffect"/>: abilities and card types are
/// separate axes (CR 113 vs CR 205); losing the creature type does not
/// automatically lose any keyword abilities.
/// </para>
///
/// <para>
/// CR 205.1a (verbatim): "Some effects change an object's card type, subtype,
/// and/or supertype but specify that the object retains a prior card type, subtype,
/// and/or supertype. In such cases, all the object's prior card types, subtypes, and
/// supertypes are retained, and the effect causes the object to gain or lose other
/// card types, subtypes, and/or supertypes."
/// </para>
/// </summary>
[OracleEffect("loseType")]
public sealed record LoseTypeEffect : ContinuousEffect
{
  /// <summary>
  /// The permanent that loses the card type. Typically
  /// <see cref="ObjectReferenceKind.Self"/> for "this card isn't a creature."
  /// </summary>
  public required ObjectReference Subject { get; init; }

  /// <summary>
  /// The card type that is lost (lowercase to match the <see cref="ObjectFilter.CardTypes"/>
  /// convention — e.g. <c>"creature"</c> for the Heliod / Erebos God template).
  /// </summary>
  public required string LostType { get; init; }
}
