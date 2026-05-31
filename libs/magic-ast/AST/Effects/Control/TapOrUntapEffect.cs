namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "tap or untap [target]" — disjunctive tap/untap effect.
/// The controller chooses whether to tap or untap the target.
/// Commonly appears as "You may tap or untap target [filter]."
/// Rule 701.26 (Tap and Untap).
/// </summary>
[OracleEffect("tapOrUntap")]
public sealed record TapOrUntapEffect : Effect
{
  public required ObjectReference Target { get; init; }
}
