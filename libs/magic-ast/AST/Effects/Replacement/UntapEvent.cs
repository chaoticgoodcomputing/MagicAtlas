namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Untap event: "would become untapped"
/// </summary>
public sealed record UntapEvent : ReplacementEvent;
