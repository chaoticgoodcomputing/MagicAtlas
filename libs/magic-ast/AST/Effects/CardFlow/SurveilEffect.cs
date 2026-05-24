namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Surveil N" - look at top N cards, put any number into graveyard, rest on top in any order.
/// Rule 701.42
/// </summary>
[OracleEffect("surveil")]
public sealed record SurveilEffect : Effect
{
  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }
}
