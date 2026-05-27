namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Attach [this permanent] to target [filter]." — the oracle-text instruction to
/// move an Equipment, Aura, or Fortification onto a qualifying object or player.
/// Rule 701.3: "To take an Aura, Equipment, or Fortification from where it currently
/// is and put it onto a specified object or player."
///
/// <para>
/// MAST records what oracle text says (the attachment instruction and its target);
/// the rules-engine mechanics of legality checks, zone-change effects, and
/// continuous effects from the attachment are engine territory
/// (per the descriptive-not-engine doctrine).
/// </para>
///
/// <para>
/// Distinct from <see cref="MagicAST.AST.Effects.Keyword.EquipEffect"/>, which
/// models the Equip keyword ability (an activated ability defined by Rule 702.6).
/// This effect models explicit oracle-text "attach" instructions, typically inside
/// triggered abilities on Equipment that auto-attach on entry.
/// </para>
/// </summary>
[OracleEffect("attach")]
public sealed record AttachEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The object to attach this permanent to.
  /// e.g., "target creature you control" →
  ///   <c>{ Kind = Target, Filter = { CardTypes = ["creature"], Controller = You } }</c>
  /// </summary>
  public required ObjectReference Target { get; init; }

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
