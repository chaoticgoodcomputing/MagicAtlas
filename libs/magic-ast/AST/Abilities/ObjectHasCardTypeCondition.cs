namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "as long as enchanted permanent is a creature" / "as long as enchanted permanent
/// is an Equipment" — a condition that checks whether a designated game object (the
/// enchanted/equipped permanent, or another back-referenced object) currently has a
/// specific card type.
///
/// <para>
/// CR 611.3a: a continuous effect from a static ability "isn't 'locked in'; it applies
/// at any given moment to whatever its text indicates" — so a type-gated grant stops
/// applying the moment the object no longer has the type. Card types are checked continuously
/// against the object's current characteristics (CR 205.2a — creature, artifact,
/// enchantment, land, etc. are the card types). MAST records the condition as
/// written — reference-not-resolution (ADR 0004): the engine reads the object's
/// current type line; MAST does not pre-evaluate it.
/// </para>
///
/// <para>
/// Card-type analogue of <see cref="ObjectHasSubtypeCondition"/> (CR 205.3m
/// subtypes): that condition checks a creature subtype ("if it's a Unicorn"); this
/// one checks a main card type ("is a creature"). Distinct axes per CR 205 — an
/// Equipment such as "Equipped creature has flying" is a SUBTYPE grant
/// (<see cref="ObjectHasSubtypeCondition"/>), whereas Rune of Flight's "enchanted
/// permanent is a creature" checks the CARD TYPE.
/// </para>
///
/// <para>
/// The <see cref="Subject"/> field names the object being checked, using the same
/// title-case <c>ObjectReferenceKind</c> vocabulary as
/// <see cref="ObjectHasSubtypeCondition.Subject"/> — typically
/// <c>"EnchantedOrEquipped"</c> for an Aura/Equipment's own "enchanted
/// permanent"/"equipped creature" self-reference.
/// </para>
/// </summary>
[ConditionKind("objectHasCardType")]
public sealed record ObjectHasCardTypeCondition : Condition
{
  /// <summary>
  /// The card type to check — e.g. <c>"creature"</c>, <c>"artifact"</c> (CR 205.2a,
  /// lowercase to match the <see cref="MagicAST.AST.References.ObjectFilter.CardTypes"/>
  /// vocabulary).
  /// </summary>
  public required string CardType { get; init; }

  /// <summary>
  /// The object whose card type is being checked — typically
  /// <c>"EnchantedOrEquipped"</c> (the enchanted permanent), matching the
  /// <c>ObjectReferenceKind</c> vocabulary.
  /// </summary>
  public required string Subject { get; init; }
}
