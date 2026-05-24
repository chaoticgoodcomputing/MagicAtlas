namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "This spell can't be countered."
/// Rule 702.64 (for Uncounterable as a keyword)
/// </summary>
[OracleEffect("cantBeCountered")]
public sealed record CantBeCounteredEffect : Effect
{
  // This effect makes the spell or ability uncounterable.
}
