namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Afterlife N (Rule 702.135). A triggered keyword ability: when this creature
/// dies, create N 1/1 white and black Spirit creature tokens with flying. MAST
/// records the keyword and its integer value; the dies-trigger and token-creation
/// are engine territory per the descriptive-not-engine doctrine. Integer-
/// parameterized keyword; mirrors BushidoEffect and AfflictEffect.
/// </summary>
[OracleEffect("afterlife")]
public sealed record AfterlifeEffect : Effect
{
  /// <summary>The afterlife value N printed on the card (e.g., "Afterlife 1" → 1).</summary>
  public required int Value { get; init; }
}
