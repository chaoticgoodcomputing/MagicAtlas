namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat damage assignment substitution: creatures assign combat damage equal to
/// their toughness rather than their power.
/// Rule 510.1a (verbatim): "Each attacking creature and each blocking creature
/// assigns combat damage equal to its power."
/// This effect records the static replacement that substitutes toughness for power
/// when assigning combat damage — the High Alert / Doran, the Siege Tower family.
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// The presence of this effect on a <c>StaticAbility</c> records that the card's
/// oracle line states that the named objects assign combat damage equal to their
/// toughness rather than their power; it does not model the runtime combat-damage
/// assignment step.
///
/// <para>
/// Distinct from <see cref="AssignDamageAsUnblockedEffect"/> (Rule 510.1c):
/// that substitution changes <em>who</em> receives the damage (blocked vs. unblocked
/// target); this substitution changes <em>how much</em> damage is assigned (toughness
/// instead of power). The two are orthogonal.
/// </para>
///
/// <para>
/// When <see cref="AppliesTo"/> is null, the effect applies to the static
/// ability's controlling object (the card itself). When set, it describes the
/// set of permanents affected — e.g., "each creature you control" (High Alert)
/// or "all creatures" (Doran) — via an <see cref="ObjectReference"/> whose
/// <see cref="ObjectReference.Filter"/> carries the card-type and controller
/// constraints.
/// </para>
/// </remarks>
[OracleEffect("assignDamageAsToughness")]
public sealed record AssignDamageAsToughnessEffect : ContinuousEffect
{
  /// <summary>
  /// The set of creatures whose combat-damage assignment is replaced. Null means
  /// the static ability's controlling object (the printed card itself); set when
  /// the oracle line specifies a broader subject ("each creature you control",
  /// "all creatures").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? AppliesTo { get; init; }
}
