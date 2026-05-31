namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Mayhem (Rule 702.187). An alternative-cost keyword allowing a card to be
/// cast from the graveyard by paying the mayhem cost, but only if it was
/// discarded this turn. Oracle form: "Mayhem [cost]". MAST records the
/// keyword's presence and the alternative cost; the discard-condition and
/// graveyard-cast mechanics are reminder text and engine territory.
///
/// <para>
/// CR 702.187b: "\"Mayhem [cost]\" means \"As long as you discarded this
/// card this turn, you may cast it from your graveyard by paying [cost]
/// rather than paying its mana cost.\" Casting a spell using its mayhem
/// ability follows the rules for paying alternative costs in rules 601.2b
/// and 601.2f-h."
/// </para>
/// </summary>
[OracleEffect("mayhem")]
public sealed record MayhemEffect : Effect
{
  /// <summary>
  /// The alternative cost paid to cast this card using its mayhem ability.
  /// Typically a <see cref="ManaCost"/>.
  /// </summary>
  public required Cost Cost { get; init; }
}
