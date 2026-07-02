namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Death event: "would die"
/// </summary>
[OracleReplacementEvent("death")]
public sealed record DeathEvent : ReplacementEvent;
