namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "search your library for [filter]"
/// </summary>
public sealed record SearchLibraryEffect : Effect
{
  [JsonPropertyName("filter")]
  public required ObjectFilter Filter { get; init; }

  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }

  [JsonPropertyName("destination")]
  public required SearchDestination Destination { get; init; }

  [JsonPropertyName("revealed")]
  public bool Revealed { get; init; }
}
