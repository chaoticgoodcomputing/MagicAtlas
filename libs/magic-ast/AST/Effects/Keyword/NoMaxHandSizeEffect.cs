namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You have no maximum hand size." / "Players have no maximum hand size." —
/// Rule 402.2. A continuous static effect that removes the maximum hand size
/// restriction for the scoped player(s), for as long as the source permanent
/// is on the battlefield; no duration or condition field is needed because the
/// effect persists by virtue of the permanent's presence (Rule 604.3).
/// </summary>
/// <remarks>
/// Keeping <see cref="Player"/> as a field (rather than baking "you"/"players"
/// into the discriminator) lets both shapes reuse this node — mirroring how
/// <see cref="MagicAST.AST.Effects.Resource.CantGainLifeEffect"/> scopes "You
/// can't gain life" vs. the symmetric "Players can't gain life." with the same
/// node and an <see cref="ObjectReference"/> field. "You have no maximum hand
/// size" (the overwhelmingly common single-controller case, e.g. Graceful
/// Adept) → <see cref="ObjectReferenceKind.You"/>. "Players have no maximum
/// hand size" (the symmetric, all-players case, e.g. Price of Knowledge) →
/// <see cref="ObjectReferenceKind.EachPlayer"/>.
/// </remarks>
[OracleEffect(
  "noMaxHandSize",
  NearDuplicateOf = new[] { "maxHandSize" },
  Reason = "SET vs REMOVE on the same rule (CR 402.2). 'maxHandSize' sets the maximum to a fixed number ('Your maximum hand size is ten'); 'noMaxHandSize' removes the restriction entirely ('You have no maximum hand size'). The 'no-' prefix inverts rather than qualifies, exactly as tap/untap. Not sprawl."
)]
public sealed record NoMaxHandSizeEffect : Effect
{
  /// <summary>
  /// Who no longer has a maximum hand size — the scope of the removal.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
