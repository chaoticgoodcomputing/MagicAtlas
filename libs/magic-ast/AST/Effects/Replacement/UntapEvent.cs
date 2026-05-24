namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Untap event: "would become untapped"
/// </summary>
[OracleReplacementEvent("untap")]
public sealed record UntapEvent : ReplacementEvent;
