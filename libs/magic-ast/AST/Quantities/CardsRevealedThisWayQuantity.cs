namespace MagicAST.AST.Quantities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A quantity equal to the number of cards <b>revealed this way</b> by a
/// <see cref="MagicAST.AST.Effects.CardFlow.RevealCardsEffect"/> earlier in the same
/// spell or ability — "where X is the number of cards revealed this way" (Scent of
/// Nightshade: "Reveal any number of black cards in your hand. Target creature gets
/// -X/-X until end of turn, where X is the number of cards revealed this way.").
///
/// <para>
/// The "this way" sibling of <see cref="CounterCountQuantity"/> (counters on an object)
/// and <see cref="CountersRemovedThisWayQuantity"/> (counters consumed by a cost): a
/// count of cards produced by the same ability's own reveal action. Reference-not-resolution
/// (ADR 0004): "this way" names the <see cref="MagicAST.AST.Effects.CardFlow.RevealCardsEffect"/>
/// in the same effect list — it is a textual link, NOT a variable threaded from the reveal —
/// so MAST records the reference and the engine evaluates the actual count at resolution.
/// Field-less, mirroring <see cref="DieRollResultQuantity"/> / <see cref="DomainQuantity"/> /
/// <see cref="AnyAmountQuantity"/>. Serializes as <c>{"QuantityType":"cardsRevealedThisWay"}</c>.
/// </para>
///
/// <para>
/// CR 701.20a: "To reveal a card, show that card to all players for a brief time."
/// </para>
/// </summary>
[OracleQuantity("cardsRevealedThisWay")]
public sealed record CardsRevealedThisWayQuantity : Quantity;
