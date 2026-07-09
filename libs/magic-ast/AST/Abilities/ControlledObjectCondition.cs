namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if you controlled that permanent" — a control-gate condition that is true when the
/// designated player controlled the referenced object immediately before it last changed
/// zones. The structured form of Boomerang Basics' "Return target nonland permanent to its
/// owner's hand. If you controlled that permanent, draw a card." — the "that permanent"
/// back-reference is the object the preceding <see cref="MagicAST.AST.Effects.ZoneChange.ReturnToHandEffect"/>
/// bounced, so by the time this condition is evaluated the permanent is no longer on the
/// battlefield.
///
/// <para>
/// The past tense ("controlled") is forced by CR 109.4: "Only objects on the stack or on the
/// battlefield have a controller. Objects that are neither on the stack nor on the battlefield
/// aren't controlled by any player." Once the permanent has been returned to hand it has no
/// controller, so the gate looks back at whether <see cref="Controller"/> controlled it just
/// before it left. Reference-not-resolution (ADR 0004): MAST records the printed gate; the
/// engine reads the object's last-known control, MAST does not pre-evaluate it.
/// </para>
///
/// <para>
/// <see cref="Reference"/> names the object being checked — typically <c>{Kind:"It"}</c>
/// ("that permanent", the immediately preceding return target). <see cref="Controller"/> is
/// the player the object must have been controlled by — typically <c>{Kind:"You"}</c> ("you",
/// the spell's controller, CR 109.5). Distinct from a <see cref="CountCondition"/> ("you
/// control a [filter]"): that counts objects matching a type/zone filter, whereas this gates
/// on a specific back-referenced object, not a filter.
/// </para>
///
/// CR 109.4 (verbatim): "Only objects on the stack or on the battlefield have a controller.
/// Objects that are neither on the stack nor on the battlefield aren't controlled by any player."
/// </summary>
[ConditionKind("controlledObject")]
public sealed record ControlledObjectCondition : Condition
{
  /// <summary>
  /// The object whose past control is being checked — typically <c>{Kind:"It"}</c>
  /// ("that permanent" from the preceding return-to-hand effect).
  /// </summary>
  public required ObjectReference Reference { get; init; }

  /// <summary>
  /// The player the referenced object must have been controlled by — typically
  /// <c>{Kind:"You"}</c> ("you", the spell's controller, CR 109.5).
  /// </summary>
  public required ObjectReference Controller { get; init; }
}
