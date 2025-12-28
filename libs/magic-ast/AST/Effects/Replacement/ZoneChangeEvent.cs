namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Zone change event: "would be put into [zone]" / "would enter [zone]"
/// </summary>
public sealed record ZoneChangeEvent : ReplacementEvent
{
  /// <summary>
  /// The destination zone.
  /// </summary>
  [JsonPropertyName("destinationZone")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Zone? DestinationZone { get; init; }

  /// <summary>
  /// The origin zone (if specified).
  /// </summary>
  [JsonPropertyName("originZone")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Zone? OriginZone { get; init; }
}
