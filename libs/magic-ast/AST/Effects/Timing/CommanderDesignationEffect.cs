namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "[Card] can be your commander."
/// Used on non-legendary permanents or planeswalkers that can be commanders.
/// </summary>
[OracleEffect("commanderDesignation")]
public sealed record CommanderDesignationEffect : Effect
{
  // This simply marks the card as being a valid commander.
}
