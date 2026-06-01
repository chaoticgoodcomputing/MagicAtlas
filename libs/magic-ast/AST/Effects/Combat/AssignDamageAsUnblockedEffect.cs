namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat damage-assignment substitution (attacker-side): oracle text states that
/// a blocked creature may "assign its combat damage as though it weren't blocked."
/// Rule 510.1c (combat-damage step; a blocked creature normally assigns its combat
/// damage to the creatures blocking it — "If exactly one creature is blocking it,
/// it assigns all its combat damage to that creature. If two or more creatures are
/// blocking it, its controller divides that creature's combat damage…"). This
/// effect records the substitution that lets the creature instead assign that
/// damage as though no creature were blocking it (i.e., to the player or
/// planeswalker it is attacking).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// The presence of this effect on a <c>StaticAbility</c> records that the card's
/// oracle line grants the named object the option to substitute its
/// damage-assignment as though unblocked; it does not model the runtime
/// damage-assignment division performed during the combat-damage step.
///
/// <para>
/// Distinct from <c>TrampleEffect</c> (Rule 702.19). Trample assigns only the
/// <i>excess</i> combat damage to the player or planeswalker — the blocking
/// creatures must first be assigned lethal damage. This effect grants a <i>full</i>
/// substitution: the creature may assign <i>all</i> its combat damage as though it
/// weren't blocked, ignoring the blockers entirely. The two are not
/// interchangeable; do not collapse one into the other.
/// </para>
/// <para>
/// <see cref="IsOptional"/> records the "You may have this creature…" phrasing
/// (Pride of Lions). When <c>true</c>, the controller chooses each combat whether
/// to use the substitution; when <c>false</c> (default), the substitution is
/// unconditional. The flag carries the oracle "you may" structurally rather than
/// as free text. (A continuous permission's "you may" is intrinsic to the grant,
/// not a one-shot decision, so it is a flag here rather than the
/// <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/> wrapper, which ADR 0005
/// reserves for one-shot action effects.)
/// </para>
/// <para>
/// When <see cref="Target"/> is null, the substitution applies to the static
/// ability's controlling object (the printed card itself), e.g. "You may have
/// this creature assign its combat damage as though it weren't blocked." When set,
/// it names a distinct object, e.g. <c>EnchantedOrEquipped</c> for Aura/Equipment
/// bodies. Mirrors <see cref="CantBlockEffect.Target"/>.
/// </para>
/// </remarks>
[OracleEffect("assignDamageAsUnblocked")]
public sealed record AssignDamageAsUnblockedEffect : ContinuousEffect
{
  /// <summary>
  /// When <c>true</c>, the oracle text grants the substitution with a "You may"
  /// prefix — the controller chooses each combat whether the creature assigns
  /// its combat damage as though unblocked (Pride of Lions). When <c>false</c>
  /// (default), the substitution is unconditional. Omitted from JSON when false
  /// so existing fixtures require no changes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool IsOptional { get; init; }

  /// <summary>
  /// The object the substitution applies to. Null means the static ability's
  /// controlling object (the printed card itself); set for Aura/Equipment
  /// bodies and global grants targeting other objects.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}
