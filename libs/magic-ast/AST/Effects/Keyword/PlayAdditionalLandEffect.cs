namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may play an additional land on each of your turns." — Rule 305.2
/// (land-play limit). A continuous static effect that grants the controller
/// permission to play one or more additional lands per turn beyond the normal
/// limit of one (CR 305.2: "A player can normally play one land during their
/// turn; however, continuous effects may increase this number.").
///
/// The subject is always "You" (the controller); no target, duration, or
/// condition field is needed because the effect persists by virtue of the
/// permanent's presence on the battlefield.
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records
/// that the card's oracle line grants its controller the option to play
/// additional lands per turn; it does not model runtime enforcement of the
/// increased limit.
///
/// <para>
/// The "You may" preamble means the effect is wrapped in an
/// <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/>: the controller
/// is not required to play the extra land — the grant is purely permissive.
/// </para>
/// <para>
/// <see cref="Count"/> is the number of additional lands granted:
/// <c>null</c> means "an additional land" (implicitly one, as in Exploration),
/// and a <see cref="LiteralQuantity"/> carries the explicit integer for cards
/// like Azusa, Lost but Seeking ("two additional lands"). This avoids
/// proliferating separate node types per count.
/// </para>
/// <para>
/// Full-sentence form on oracle text, not a printed keyword — no
/// <c>KeywordSource</c> is recorded on the containing ability. Covers
/// Exploration, Azusa, Oracle of Mul Daya, and similar land-drop expanders.
/// </para>
/// </remarks>
[OracleEffect("playAdditionalLand")]
public sealed record PlayAdditionalLandEffect : Effect
{
  /// <summary>
  /// Number of additional lands the controller may play. Omitted (null) when
  /// the oracle text says "an additional land" (implicit count of one, as in
  /// Exploration). Populated with a <see cref="LiteralQuantity"/> when an
  /// explicit number is printed ("two additional lands" → 2).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }
}
