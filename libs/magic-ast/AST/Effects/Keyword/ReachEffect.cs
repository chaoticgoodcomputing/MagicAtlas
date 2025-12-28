namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Reach effect: this creature can block creatures with flying.
/// Rule 702.17
/// </summary>
public sealed record ReachEffect : Effect
{
  // Reach is a static ability with fixed semantics defined by rule 702.17.
}
