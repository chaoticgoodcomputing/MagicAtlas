namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Reveal [count] [cards] in [zone]" — a player shows cards matching a filter from a
/// zone to all players (the Scent of Nightshade family: "Reveal any number of black
/// cards in your hand"). Distinct from the specialized library-reveal effects
/// (<see cref="RevealUntilEffect"/>, <see cref="RevealTopPutMatchingToHandEffect"/>,
/// etc.): this is the general "reveal a chosen set of cards you already hold" action
/// whose <see cref="Count"/> often defines a later variable ("where X is the number of
/// cards revealed this way" — see <see cref="CardsRevealedThisWayQuantity"/>).
///
/// <para>
/// CR 701.20a (verbatim): "To reveal a card, show that card to all players for a brief
/// time. If an effect causes a card to be revealed, it remains revealed for as long as
/// necessary to complete the parts of the effect that card is relevant to."
/// </para>
/// </summary>
[OracleEffect("revealCards")]
public sealed record RevealCardsEffect : Effect
{
  /// <summary>
  /// The player who reveals — "you" for "Reveal … in your hand".
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// How many cards are revealed — "any number" is an <see cref="AnyAmountQuantity"/>.
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// The zone the revealed cards come from — <see cref="Zone.Hand"/> for "in your hand".
  /// </summary>
  public required Zone Zone { get; init; }

  /// <summary>
  /// Which cards qualify to be revealed — "black cards" is
  /// <c>CardTypes=["card"]</c> + <c>Colors=["B"]</c>. Null when the text places no
  /// restriction ("reveal a card").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }
}
