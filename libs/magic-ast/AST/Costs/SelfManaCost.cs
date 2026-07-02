namespace MagicAST.AST.Costs;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "the card's mana cost" — a self-referential cost that stands for the
/// mana cost of the card on which the ability is printed (or granted to),
/// rather than a specific fixed mana-cost value.
///
/// <para>
/// Used by Underworld Breach (THB, CR 702.139): "The escape cost is equal to
/// the card's mana cost plus exile three other cards from your graveyard."
/// Each nonland graveyard card's escape cost has a mana component equal to
/// that card's own printed mana cost — not to the enchantment's cost. MAST
/// records this as a reference; the engine resolves the card's mana cost at
/// cast time (CR 202.1 — "The mana cost of an object represents what a
/// player must spend from their mana pool to cast that card."). No fields
/// are needed: the reference is always "the card's own mana cost" and the
/// card is always the context object.
/// </para>
///
/// <para>
/// Serialised as <c>{ "CostType": "selfMana" }</c>. Sits alongside other
/// alternative-cost primitives under <see cref="Cost"/>.
/// </para>
/// </summary>
[OracleCost("selfMana")]
public sealed record SelfManaCost : Cost;
