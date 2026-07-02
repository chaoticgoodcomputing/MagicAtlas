namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-attack restriction (attacker-side): oracle text states that a
/// creature "can't attack." Rule 508.1c (declare-attackers step; attacking
/// restrictions constrain the set of legal attacker declarations the active
/// player can make).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records that
/// the card's oracle line imposes a "can't attack" restriction on the named
/// object; it does not model the runtime decision that the active player must
/// make at declare-attackers.
///
/// <para>
/// This is the dual of <see cref="MustAttackEffect"/> — same rule (508.1c),
/// opposite polarity:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="MustAttackEffect"/> is an attacker-side <i>requirement</i>:
///     the listed creature must be declared as an attacker when it legally
///     can.
///   </description></item>
///   <item><description>
///     <see cref="CantAttackEffect"/> is an attacker-side <i>restriction</i>:
///     the listed creature is excluded from the set of legal attacker
///     declarations.
///   </description></item>
/// </list>
/// <para>
/// Directly parallels <see cref="CantBlockEffect"/> (Rule 509.1c) on the
/// blocker side. The two are often paired in a single oracle clause, e.g.
/// "Enchanted creature can't attack or block." Per the multi-effect-per-clause
/// doctrine that clause yields two <see cref="Effect"/> records on one
/// <c>StaticAbility</c> — a <see cref="CantAttackEffect"/> and a
/// <see cref="CantBlockEffect"/> — not a combined node.
/// </para>
/// <para>
/// When <see cref="Target"/> is null, the restriction applies to the static
/// ability's controlling object (the card the ability is printed on),
/// e.g. "This creature can't attack." When set, it names a distinct object,
/// e.g. <c>EnchantedOrEquipped</c> for the Aura body
/// "Enchanted creature can't attack."
/// </para>
/// <para>
/// When <see cref="UnlessDefendingControls"/> is set, the restriction is
/// conditioned on the defending player's board state: the creature is permitted
/// to attack only when the stated condition holds (e.g. "unless defending player
/// controls an Island"). This is the attacker-side dual of landwalk — where
/// landwalk records that a blocker-side restriction lifts when defending player
/// controls the land, this records that an attacker-side restriction applies
/// unconditionally except when defending player controls the land.
/// Rule 508.1c; descriptive only — MAST does not evaluate the condition.
/// </para>
/// </remarks>
[OracleEffect("cantAttack")]
public sealed record CantAttackEffect : ContinuousEffect
{
  /// <summary>
  /// The object the restriction applies to. Null means the static ability's
  /// controlling object (the printed card itself); set for Aura/Equipment
  /// bodies and global restrictions.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

  /// <summary>
  /// Board-state condition that lifts this attack restriction. When set, the
  /// oracle line reads "can't attack unless defending player controls [permanent]"
  /// and the condition uses <see cref="EvasionConditionType.DefendingPlayerControls"/>
  /// with the land-type (or other permanent type) on
  /// <see cref="EvasionCondition.PermanentFilter"/>.
  ///
  /// <para>
  /// Reuses <see cref="EvasionCondition"/> from the landwalk family because the
  /// "defending player controls [land]" predicate is identical in both directions:
  /// landwalk records that a blocker-side restriction is lifted when the condition
  /// holds; this records that an attacker-side restriction applies unless the same
  /// condition holds. The shared predicate type keeps the domain model coherent and
  /// avoids a separate near-duplicate type.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public EvasionCondition? UnlessDefendingControls { get; init; }

  /// <summary>
  /// "alone" qualifier: when <c>true</c>, the oracle line reads "can't attack
  /// alone" — the restriction applies only when this object would be the sole
  /// attacker, i.e. it is lifted whenever at least one other creature is also
  /// declared as an attacker.
  ///
  /// <para>
  /// CR 508.1 (excerpt): "First, the active player declares attackers. This
  /// turn-based action doesn't use the stack…" — "alone" describes the
  /// declared-attackers set: the restriction bites precisely when this object
  /// is the only member of that set. MAST records what the word means (the
  /// restriction is conditional on no other creature also attacking); it does
  /// not model the declare-attackers turn-based action itself.
  /// </para>
  /// <para>
  /// Shared with <see cref="CantBlockEffect.Alone"/>. "This creature can't
  /// attack or block alone." is ONE restriction whose "alone" qualifier applies
  /// to both halves; per the multi-effect-per-clause doctrine that clause yields
  /// a <see cref="CantAttackEffect"/> and a <see cref="CantBlockEffect"/>, each
  /// carrying <c>Alone = true</c> — not a combined node and not a free-text
  /// "alone" string.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool Alone { get; init; }

  /// <summary>
  /// "only alone" qualifier: when <c>true</c>, the oracle line reads "can only
  /// attack alone" — the restriction applies when this object would NOT be the
  /// sole attacker, i.e. the creature may only be declared as an attacker when
  /// no other creatures are also attacking.
  ///
  /// <para>
  /// CR 508.1 (excerpt): "First, the active player declares attackers. This
  /// turn-based action doesn't use the stack…" — "only alone" is the dual of
  /// <see cref="Alone"/>: where <see cref="Alone"/> lifts the restriction when
  /// other creatures also attack, <see cref="OnlyAlone"/> applies the restriction
  /// precisely when other creatures are also attacking. MAST records what the
  /// words mean; the declare-attackers legality check is engine territory.
  /// </para>
  ///
  /// <para>
  /// This qualifier is distinct from <see cref="Alone"/>. "This creature can only
  /// attack alone." (Master of Cruelties) yields a <see cref="CantAttackEffect"/>
  /// with <c>OnlyAlone = true</c>; "This creature can't attack alone." (Loyal
  /// Pegasus) yields a <see cref="CantAttackEffect"/> with <c>Alone = true</c>.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool OnlyAlone { get; init; }
}
