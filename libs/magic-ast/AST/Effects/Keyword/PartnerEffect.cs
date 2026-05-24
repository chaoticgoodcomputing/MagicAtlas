namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Partner effects: Partner, Partner with [name], Choose a Background, Friends forever, Doctor's companion.
/// Rule 702.124
/// </summary>
[OracleEffect("partner")]
public sealed record PartnerEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The type of partner ability.
  /// </summary>
  [JsonPropertyName("partnerType")]
  public required PartnerType PartnerType { get; init; }

  /// <summary>
  /// For "Partner with [name]", the specific partner card name.
  /// </summary>
  [JsonPropertyName("partnerName")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? PartnerName { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  [JsonPropertyName("isOptional")]
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonPropertyName("ifYouDo")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonPropertyName("duration")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonPropertyName("unlessClause")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
