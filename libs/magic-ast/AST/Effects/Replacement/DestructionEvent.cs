namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Destruction event: "would be destroyed"
/// </summary>
[OracleReplacementEvent("destruction")]
public sealed record DestructionEvent : ReplacementEvent;
