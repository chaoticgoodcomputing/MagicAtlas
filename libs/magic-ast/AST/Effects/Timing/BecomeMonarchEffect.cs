namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[Player] becomes the monarch." Rule 716 — the monarch designation is a
/// per-game status assigned to exactly one player at a time. MAST records the
/// descriptive instruction; the consequent draw-at-end-step / combat-damage
/// transfer rules are engine territory.
/// </summary>
[OracleEffect("becomeMonarch")]
public sealed record BecomeMonarchEffect : Effect
{
  /// <summary>
  /// The player who becomes the monarch.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
