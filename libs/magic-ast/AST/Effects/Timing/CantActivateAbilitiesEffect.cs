namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Activation-lock restriction: oracle text states that a permanent's
/// activated abilities "can't be activated." Rule 602.5 — a player can
/// activate an activated ability only when the rules and effects allow it;
/// this effect is a continuous restriction that prevents any activated ability
/// on the named object from being put on the stack.
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records
/// that the card's oracle line imposes an activation lock on the named
/// object's activated abilities; it does not model the runtime enforcement.
///
/// <para>
/// This effect most commonly appears as the third clause of the Arrest /
/// Lawmage's Binding pattern, bundled in one
/// <c>StaticAbility.Effects</c> list alongside
/// <see cref="MagicAST.AST.Effects.Combat.CantAttackEffect"/> and
/// <see cref="MagicAST.AST.Effects.Combat.CantBlockEffect"/>:
/// "Enchanted creature can't attack or block, and its activated abilities
/// can't be activated." Per the multi-effect-per-clause doctrine that
/// single oracle line yields three <see cref="MagicAST.AST.Effects.Effect"/>
/// records on one <c>StaticAbility</c> — not three separate ability nodes.
/// </para>
///
/// <para>
/// When <see cref="Target"/> is null, the restriction applies to the static
/// ability's controlling object (the card the ability is printed on).
/// When set, it names a distinct object, e.g.
/// <c>EnchantedOrEquipped</c> for the Aura body
/// "its activated abilities can't be activated."
/// </para>
///
/// <para>
/// Distinct from <see cref="CantBeCastEffect"/> (which prevents spells from
/// being cast) and from Split Second (Rule 702.61, which prevents activation
/// while a spell with split second is on the stack). This effect is a
/// continuous oracle-text restriction on a specific permanent, not a
/// stack-state restriction.
/// </para>
/// </remarks>
[OracleEffect("cantActivateAbilities")]
public sealed record CantActivateAbilitiesEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The object whose activated abilities are locked out. Null means the
  /// static ability's controlling object (the printed card itself); set for
  /// Aura/Equipment bodies, e.g. <c>EnchantedOrEquipped</c>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
