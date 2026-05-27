namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Enchanted [type] is a(n) [Subtype]." — layer-4 (CR 613.1d) subtype-changing
/// continuous effect applied to the attached permanent. Describes the oracle-text
/// declaration that the enchanted permanent's subtypes are replaced by (or set to)
/// the specified set. No Duration: the effect persists while the Aura remains
/// attached (Rule 702.5 / 613.1d).
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>Spreading Seas — "Enchanted land is an Island." → Subtypes: ["Island"]</item>
///   <item>Convincing Mirage — "Enchanted land is a Plains." → Subtypes: ["Plains"]</item>
///   <item>Phantasmal Terrain — various basic land type shapes.</item>
/// </list>
/// </para>
///
/// <para>
/// MAST is descriptive: this node records what the oracle line says. The rules
/// engine is responsible for how layer-4 subtype changes interact with other
/// continuous effects (CR 613.7), the implicit basic land mana ability grant
/// (CR 305.6), and landwalk evasion (CR 702.14).
/// </para>
/// </summary>
[OracleEffect("changeSubtype")]
public sealed record ChangeSubtypeEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The permanent whose subtype is being changed.
  /// Typically <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> for Aura lines.
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The subtype(s) the target is set to. For the Spreading Seas pattern this
  /// is a single element: <c>["Island"]</c>. Multiple subtypes are possible for
  /// effects that set a permanent to several subtypes simultaneously.
  /// </summary>
  public required IReadOnlyList<string> Subtypes { get; init; }

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
