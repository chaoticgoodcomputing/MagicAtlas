namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Trample effect: excess combat damage can be assigned to the defending player or planeswalker.
/// "If this creature would assign enough damage to its blockers to destroy them, you may have it
/// assign the rest of its damage to the player or planeswalker it's attacking."
/// Rule 702.19
/// </summary>
public sealed record TrampleEffect : Effect
{
  // Trample is a static ability with fixed semantics defined by rule 702.19.
  // The interpreter handles: excess damage assignment to defending player/planeswalker.
}
