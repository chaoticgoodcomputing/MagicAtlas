namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-block restriction (blocker-side): oracle text states that a creature
/// "can't block." Rule 509.1c (declare-blockers step; blocking restrictions
/// constrain the set of legal blocker declarations the defending player can
/// make).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records that
/// the card's oracle line imposes a "can't block" restriction on the named
/// object; it does not model the runtime decision that the defending player
/// must make at declare-blockers.
///
/// <para>
/// This is the dual of <see cref="MustBlockEffect"/> — same rule (509.1c),
/// opposite polarity:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="MustBlockEffect"/> is a blocker-side <i>requirement</i>:
///     the listed creature must be declared as a blocker when it legally can.
///   </description></item>
///   <item><description>
///     <see cref="CantBlockEffect"/> is a blocker-side <i>restriction</i>:
///     the listed creature is excluded from the set of legal blocker
///     declarations.
///   </description></item>
/// </list>
/// <para>
/// Distinct from <see cref="MustBeBlockedEffect"/>, which is an attacker-side
/// requirement under the same rule. Distinct from
/// <c>DefenderEffect</c> (Rule 702.3) — that's the formal keyword ability
/// (printed as "Defender") which also imposes a can't-attack restriction.
/// "This creature can't block." is the sentence form on cards that aren't
/// formally Defenders.
/// </para>
/// <para>
/// When <see cref="Target"/> is null, the restriction applies to the static
/// ability's controlling object (the card the ability is printed on),
/// e.g. "This creature can't block." When set, it names a distinct object,
/// e.g. <c>EnchantedOrEquipped</c> for the Aura body
/// "Enchanted creature can't block." Mirrors
/// <see cref="CantAttackEffect.Target"/>.
/// </para>
/// </remarks>
[OracleEffect("cantBlock")]
public sealed record CantBlockEffect : ContinuousEffect
{
  /// <summary>
  /// The object the restriction applies to. Null means the static ability's
  /// controlling object (the printed card itself); set for Aura/Equipment
  /// bodies and global restrictions.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

  /// <summary>
  /// "alone" qualifier: when <c>true</c>, the oracle line reads "can't block
  /// alone" — the restriction applies only when this object would be the sole
  /// blocker, i.e. it is lifted whenever at least one other creature is also
  /// declared as a blocker.
  ///
  /// <para>
  /// CR 509.1 (excerpt): "First, the defending player declares blockers. This
  /// turn-based action doesn't use the stack…" — "alone" describes the
  /// declared-blockers set: the restriction bites precisely when this object is
  /// the only member of that set. MAST records what the word means (the
  /// restriction is conditional on no other creature also blocking); it does
  /// not model the declare-blockers turn-based action itself.
  /// </para>
  /// <para>
  /// Mirrors <see cref="CantAttackEffect.Alone"/>. "This creature can't attack
  /// or block alone." is ONE restriction whose "alone" qualifier applies to both
  /// halves; per the multi-effect-per-clause doctrine that clause yields a
  /// <see cref="CantAttackEffect"/> and a <see cref="CantBlockEffect"/>, each
  /// carrying <c>Alone = true</c> — not a combined node and not a free-text
  /// "alone" string.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool Alone { get; init; }
}
