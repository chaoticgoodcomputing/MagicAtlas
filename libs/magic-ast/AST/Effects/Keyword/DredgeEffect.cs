namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Dredge N (Rule 702.52). A keyword ability that lets a player return a card
/// from their graveyard to their hand. Oracle form: "Dredge N (If you would
/// draw a card, you may mill N cards instead. If you do, return this card from
/// your graveyard to your hand.)".
///
/// <para>
/// MAST records the keyword's presence and its integer value (the number of
/// cards milled to return this card); the draw-replacement choice and
/// mill-and-return mechanics are engine territory.
/// </para>
///
/// <para>
/// Integer-parameterized keyword; mirrors <see cref="BushidoEffect"/> and the
/// Modular/Backup/Afflict family. <see cref="Value"/> is the dredge number
/// lifted from the printed oracle text.
/// </para>
/// </summary>
[OracleEffect("dredge")]
public sealed record DredgeEffect : Effect
{
  /// <summary>The dredge value N printed on the card (e.g., "Dredge 3" → 3).</summary>
  public required int Value { get; init; }
}
