namespace MagicAST.AST.Quantities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "the rest" / "the remaining [cards]" — whatever is left over after the other
/// named shares of a distribution have been accounted for (CR 701.23e-style
/// multi-destination search text: "Put one of them onto the battlefield ...
/// Put two of them onto the battlefield ... and the rest into your hand").
/// Distinct from <see cref="AnyAmountQuantity"/> (a free, unbounded player
/// CHOICE, e.g. "remove any number of counters"): a remainder is not chosen — it
/// is exactly the found/available total minus every other share already
/// assigned in the same distribution (e.g.
/// <see cref="MagicAST.AST.Effects.ZoneChange.SearchLibraryEffect.Placements"/>).
/// Field-less; the sibling placements in the same list supply the amounts being
/// subtracted, so there is nothing to parameterise here beyond "what's left".
/// </summary>
[OracleQuantity("remainder")]
public sealed record RemainderQuantity : Quantity;
