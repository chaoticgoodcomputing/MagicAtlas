namespace MagicAST.AST.Effects.Damage;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Damage can't be prevented." (Leyline of Punishment) — a global prevention-lock
/// static that nullifies every damage-prevention effect (CR 615.1: prevention
/// effects), including "prevent [damage]" shields like <see cref="PreventDamageEffect"/>,
/// Circles/Runes of Protection, and Fog effects. Written as a plain static
/// statement (CR 604.1: "Static abilities do something all the time rather than
/// being activated or triggered. They are written as statements, and they're
/// simply true."), it is a rules-of-the-game-modifying continuous effect
/// (CR 611.1) — unlike <see cref="MagicAST.AST.Effects.Resource.CantGainLifeEffect"/>
/// and <see cref="MagicAST.AST.Effects.Resource.CantLoseGameForZeroLifeEffect"/>,
/// which scope their lock to a named <c>Player</c>, this lock is unconditional and
/// global (no player, source, or recipient is named in the oracle text), so no
/// scope field is carried — mirroring the marker-only shape of
/// <see cref="MagicAST.AST.Effects.Keyword.CantBeCounteredEffect"/>.
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not the replacement/prevention-effect
/// application machinery that CR 615 defines. This effect records only that damage
/// prevention is locked out; it does NOT model how the game engine would suppress
/// a live prevention shield.
/// </remarks>
[OracleEffect("cantPreventDamage")]
public sealed record CantPreventDamageEffect : Effect
{
  // Global, unconditional lock — no fields. "Damage can't be prevented" names no
  // player, source, or recipient scope.
}
