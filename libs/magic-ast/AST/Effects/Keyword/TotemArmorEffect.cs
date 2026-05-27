namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Totem armor (Rule 702.102). A replacement-effect keyword printed on Auras
/// as "Umbra armor (If enchanted creature would be destroyed, instead remove
/// all damage from it and destroy this Aura.)". Oracle text uses "Umbra armor"
/// but the comp-rules name is "totem armor"; the discriminator uses "totemArmor"
/// per the comp-rules term. MAST records the keyword's presence; the
/// replacement-effect semantics are engine territory per the
/// descriptive-not-engine doctrine.
///
/// <para>
/// Parameterless keyword marker; mirrors the EvolveEffect and FlankingEffect
/// shape.
/// </para>
/// </summary>
[OracleEffect("totemArmor")]
public sealed record TotemArmorEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
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
