namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Put [target] on the bottom of its owner's library."
/// A zone-change effect that moves the target from its current zone to the bottom of
/// its owner's library. CR 400.7 (an object moving zones becomes a new object) and
/// CR 401 (library ordering rules — 401.2 face-down single pile, 401.4 multiple cards
/// to the same position may be arranged by the owner, 401.7 the "Nth from the top"
/// fallback lands on the bottom when the library is shorter than N) govern resolution;
/// MAST records the destination descriptively. Distinct from
/// <see cref="PutOnTopOfLibraryEffect"/> (destination is the top, not the bottom) and
/// from <see cref="PutIntoLibraryAtPositionEffect"/> (an ordinal position counted from
/// the top, not the bottom).
/// </summary>
[OracleEffect("putOnBottomOfLibrary")]
public sealed record PutOnBottomOfLibraryEffect : Effect
{
  public required ObjectReference Target { get; init; }
}
