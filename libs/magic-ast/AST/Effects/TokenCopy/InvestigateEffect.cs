namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "investigate" — Rule 701.28. Create a Clue token (an artifact with
/// "{2}, Sacrifice this artifact: Draw a card."). MAST records the keyword
/// action; the Clue token is conventionally inferred from rules text.
/// </summary>
[OracleEffect("investigate")]
public sealed record InvestigateEffect : Effect
{
  /// <summary>
  /// How many Clue tokens to create. Defaults to one if omitted.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }
}
