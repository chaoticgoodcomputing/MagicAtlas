namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[object] is attached to a creature" — the attachment-state gate read from the
/// ATTACHED object's side: it holds while the referenced object (an Equipment, Aura,
/// Fortification, or a Reconfigure artifact) is currently attached to a permanent
/// matching <see cref="AttachedTo"/> (The Reality Chip: "As long as The Reality Chip is
/// attached to a creature, you may play lands and cast spells from the top of your
/// library.").
///
/// <para>
/// The converse of <see cref="ObjectIsEquippedCondition"/> ("that creature is equipped",
/// read from the HOST's side — is a creature carrying an Equipment): this checks whether
/// the attachment itself is attached, and to what. <see cref="Reference"/> names the
/// attached object being checked — typically <see cref="ObjectReferenceKind.Self"/> (the
/// source, named by its own card name, e.g. "The Reality Chip") — and
/// <see cref="AttachedTo"/> is the category of host it must be attached to ("a creature"
/// → <c>{CardTypes:["creature"]}</c>). A boolean predicate on one object's current
/// attachment state, the attachment-side sibling of the host-side
/// <see cref="ObjectIsEquippedCondition"/>, the zone-state
/// <see cref="ObjectInZoneCondition"/>, and the combat-state
/// <see cref="SourceCombatStateCondition"/>.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed attachment gate; the
/// engine reads whether the referenced object is actually attached to a matching host
/// (CR 701.3 — attach), MAST does not pre-evaluate it. Structured to this dedicated
/// <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 701.3a (verbatim): "To attach an Aura, Equipment, or Fortification to an object or
/// player means to take it from where it currently is and put it onto that object or
/// player."
/// CR 702.151 (Reconfigure): a Reconfigure permanent (The Reality Chip) can be attached
/// to a creature you control; while attached, it isn't a creature.
/// </summary>
[ConditionKind("objectAttached")]
public sealed record ObjectAttachedCondition : Condition
{
  /// <summary>
  /// The attached object whose attachment state is being checked — typically
  /// <c>{Kind:"Self"}</c> (the source, named by its own card name).
  /// </summary>
  public required ObjectReference Reference { get; init; }

  /// <summary>
  /// The category of host the object must be attached to — "a creature" →
  /// <c>{CardTypes:["creature"]}</c>.
  /// </summary>
  public required ObjectFilter AttachedTo { get; init; }
}
