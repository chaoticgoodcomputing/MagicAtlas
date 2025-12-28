namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Vigilance effect: attacking doesn't cause this creature to tap.
/// "Attacking doesn't cause this creature to tap."
/// </summary>
public sealed record VigilanceEffect : Effect
{
  // Vigilance is a simple marker effect with no parameters.
}
