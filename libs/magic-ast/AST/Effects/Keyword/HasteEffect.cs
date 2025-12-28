namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Haste effect: this creature can attack and use tap abilities as soon as it comes under your control.
/// "This creature can attack and {T} as soon as it comes under your control."
/// Rule 702.10
/// </summary>
public sealed record HasteEffect : Effect
{
  // Haste is a static ability with fixed semantics defined by rule 702.10.
  // The interpreter handles: removes summoning sickness restrictions.
}
