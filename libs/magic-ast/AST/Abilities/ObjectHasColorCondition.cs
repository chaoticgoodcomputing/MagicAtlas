namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "as long as enchanted creature is black" / "as long as enchanted creature is green" — a
/// condition that checks whether a designated game object currently has a specific color
/// (CR 105.1: the five colors are white, blue, black, red, green). Gift of the Deity:
/// "As long as enchanted creature is black, it gets +1/+1 and has deathtouch." and the
/// parallel green clause.
///
/// <para>
/// The color analogue of <see cref="ObjectHasCardTypeCondition"/> (a main card type,
/// CR 205.2a) and <see cref="ObjectHasSubtypeCondition"/> (a creature subtype, CR 205.3m):
/// this condition instead reads a COLOR characteristic (CR 105). Colors are checked
/// continuously against the object's current characteristics (CR 611.3a — a static
/// ability's continuous effect "isn't 'locked in'; it applies at any given moment to
/// whatever its text indicates"), so the grant stops applying the moment the enchanted
/// creature is no longer that color. MAST records the condition as written —
/// reference-not-resolution (ADR 0004): the engine reads the object's current colors;
/// MAST does not pre-evaluate it.
/// </para>
///
/// <para>
/// The <see cref="Subject"/> field names the object being checked, using the same
/// title-case <c>ObjectReferenceKind</c> vocabulary as
/// <see cref="ObjectHasCardTypeCondition.Subject"/> — typically
/// <c>"EnchantedOrEquipped"</c> for an Aura/Equipment's own "enchanted creature".
/// </para>
/// </summary>
[ConditionKind("objectHasColor")]
public sealed record ObjectHasColorCondition : Condition
{
  /// <summary>
  /// The color to check, as a single-letter code (<c>"W"</c>, <c>"U"</c>, <c>"B"</c>,
  /// <c>"R"</c>, <c>"G"</c>; CR 105.1), matching the
  /// <see cref="MagicAST.AST.References.ObjectFilter.Colors"/> vocabulary. True when the
  /// subject currently has that color.
  /// </summary>
  public required string Color { get; init; }

  /// <summary>
  /// The object whose color is being checked — typically <c>"EnchantedOrEquipped"</c>
  /// (the enchanted creature), matching the <c>ObjectReferenceKind</c> vocabulary.
  /// </summary>
  public required string Subject { get; init; }
}
