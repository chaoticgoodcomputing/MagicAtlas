namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "as long as it's legendary" — a condition that checks whether a designated game object
/// currently has a specific supertype (CR 205.4a: the supertypes are basic, legendary, snow,
/// world, and ongoing). Toralf's Hammer: "Equipped creature gets +3/+0 as long as it's
/// legendary."
///
/// <para>
/// The supertype analogue of <see cref="ObjectHasCardTypeCondition"/> (a main card type,
/// CR 205.2a) and <see cref="ObjectHasSubtypeCondition"/> (a creature subtype, CR 205.3m):
/// distinct axes per CR 205 — "legendary" is a SUPERTYPE, not a card type or subtype, so it
/// cannot be checked by either sibling without misclassifying it. Supertypes are read
/// continuously against the object's current characteristics (CR 611.3a — the continuous
/// effect "applies at any given moment to whatever its text indicates"), so the +3/+0 stops
/// the moment the equipped creature is no longer legendary. MAST records the condition as
/// written — reference-not-resolution (ADR 0004): the engine reads the object's current
/// type line; MAST does not pre-evaluate it.
/// </para>
///
/// <para>
/// The <see cref="Subject"/> field names the object being checked, using the same
/// title-case <c>ObjectReferenceKind</c> vocabulary as
/// <see cref="ObjectHasCardTypeCondition.Subject"/> — <c>"It"</c> for the bare "it's
/// legendary" pronoun (Toralf's Hammer's "it" = the equipped creature named by the
/// preceding "Equipped creature gets …" clause).
/// </summary>
[ConditionKind("objectHasSupertype")]
public sealed record ObjectHasSupertypeCondition : Condition
{
  /// <summary>
  /// The supertype to check — e.g. <c>"Legendary"</c> (CR 205.4a, title-cased to match the
  /// <see cref="MagicAST.AST.References.ObjectFilter.Supertypes"/> vocabulary). True when the
  /// subject currently has that supertype.
  /// </summary>
  public required string Supertype { get; init; }

  /// <summary>
  /// The object whose supertype is being checked — typically <c>"It"</c> (the bare pronoun),
  /// matching the <c>ObjectReferenceKind</c> vocabulary.
  /// </summary>
  public required string Subject { get; init; }
}
