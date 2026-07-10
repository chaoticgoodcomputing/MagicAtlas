namespace MagicAST.AST.Effects.Counter;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[object] can't have counters put on it/them" — a static counter-prohibition
/// (CR 122: counters). Prevents any counter (or, when scoped, a specific kind of
/// counter) from being placed on the named object(s).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// The presence of this effect on a <c>StaticAbility</c> records that the card's
/// oracle line imposes a "can't have counters put on it" lock on the named
/// object; it does not model the runtime replacement/prevention machinery that
/// stops the counter placement.
///
/// <para>
/// When <see cref="Target"/> is null, the prohibition applies to the static
/// ability's controlling object (the card the ability is printed on), e.g.
/// "This creature can't have counters put on it." (Melira's Keepers). Mirrors
/// the established nullable-Target-means-Self convention used by
/// <see cref="MagicAST.AST.Effects.Combat.CantAttackEffect.Target"/> and
/// <see cref="MagicAST.AST.Effects.Combat.CantBeBlockedEffect.Target"/>. When
/// set, it names a distinct object or group, e.g. "Other creatures you control
/// can't have counters put on them."
/// </para>
///
/// <para>
/// <see cref="CounterType"/> is null when the oracle text says "counters"
/// unqualified — the prohibition then covers every counter kind. When the
/// oracle text names a specific kind ("+1/+1 counters", "-1/-1 counters"), that
/// kind is recorded and the prohibition is scoped to it, matching the shape of
/// <see cref="PutCountersEffect.CounterType"/> / <see cref="RemoveCountersEffect.CounterType"/>.
/// </para>
/// </remarks>
[OracleEffect("cantHaveCounters")]
public sealed record CantHaveCountersEffect : ContinuousEffect
{
  /// <summary>
  /// The object(s) the prohibition applies to. Null means the static ability's
  /// controlling object (the printed card itself) — "This creature can't have
  /// counters put on it."
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

  /// <summary>
  /// The counter kind the prohibition is scoped to (e.g. "+1/+1", "-1/-1").
  /// Null means unrestricted — "counters" unqualified in the oracle text —
  /// every counter kind is prohibited.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? CounterType { get; init; }
}
