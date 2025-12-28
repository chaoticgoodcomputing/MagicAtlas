namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Partner effects: Partner, Partner with [name], Choose a Background, Friends forever, Doctor's companion.
/// Rule 702.124
/// </summary>
public sealed record PartnerEffect : Effect
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
}
