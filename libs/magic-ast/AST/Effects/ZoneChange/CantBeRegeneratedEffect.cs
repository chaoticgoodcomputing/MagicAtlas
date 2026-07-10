namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[object] can't be regenerated [this turn]." — a standalone regeneration-denial
/// restriction (Rule 701.19; regeneration shields). Unlike the <c>CantBeRegenerated</c>
/// modifier carried directly on a resolving <see cref="DestroyEffect"/> ("Destroy target
/// creature. It can't be regenerated." — a one-shot rider on a specific destruction), this
/// node records an independent, freestanding effect that forbids ANY regeneration shield
/// from being created for/applied to the named object for the stated duration, regardless
/// of what causes the destruction. Clergy of the Holy Nimbus: "This creature can't be
/// regenerated this turn."
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces. The
/// presence of this effect records the "can't be regenerated" restriction; it does not
/// model the runtime shield-suppression machinery.
///
/// <para>
/// When <see cref="Target"/> is null, the restriction applies to the static/activated
/// ability's controlling object (the card the ability is printed on) — mirroring the
/// established nullable-Target-means-Self convention used by
/// <see cref="MagicAST.AST.Effects.Combat.CantAttackEffect.Target"/> and
/// <see cref="MagicAST.AST.Effects.Counter.CantHaveCountersEffect.Target"/>.
/// </para>
/// <para>
/// The "this turn" window is carried on the inherited <see cref="ContinuousEffect.Duration"/>
/// (<c>UntilTimeDuration.EndOfTurn</c>), matching the sibling turn-scoped restrictions
/// <see cref="MagicAST.AST.Effects.Combat.CantAttackEffect"/>/<see cref="MagicAST.AST.Effects.Combat.CantBeBlockedEffect"/>
/// via their <c>…ThisTurnEffectRule</c> parsers.
/// </para>
/// </remarks>
[OracleEffect("cantBeRegenerated")]
public sealed record CantBeRegeneratedEffect : ContinuousEffect
{
  /// <summary>
  /// The object the restriction applies to. Null means the ability's controlling
  /// object (the printed card itself), e.g. "This creature can't be regenerated
  /// this turn."
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}
