namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "return [target] to its owner's graveyard" — a plain zone-change move from
/// wherever the referenced card currently is (e.g. exile) into its owner's
/// graveyard. CR 400.1/CR 400.2 (zone changes); used e.g. by the "leaves the
/// battlefield" delayed cleanup on a copy-of-an-exiled-card token (Hofri
/// Ghostforge: "return the exiled card to its owner's graveyard"). Distinct from
/// <see cref="DestroyEffect"/>/<see cref="SacrificeEffect"/> (both of which move a
/// permanent from the BATTLEFIELD to its owner's graveyard as a side effect of a
/// named game action) — this node names the graveyard-bound zone change directly,
/// with no battlefield-only precondition, so it also applies to cards moving from
/// exile.
/// </summary>
[OracleEffect("returnToGraveyard")]
public sealed record ReturnToGraveyardEffect : Effect
{
  public required ObjectReference Target { get; init; }
}
