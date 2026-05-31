namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Put [target] on top of its owner's library."
/// A zone-change effect that moves the target from its current zone to the top of
/// its owner's library. Rule 701 (general zone-change actions); distinct from
/// ReturnToHandEffect in destination zone. Most commonly appears on blue bounce
/// variants (e.g. Time Ebb, Temporal Spring).
/// </summary>
[OracleEffect("putOnTopOfLibrary")]
public sealed record PutOnTopOfLibraryEffect : Effect
{
  public required ObjectReference Target { get; init; }
}
