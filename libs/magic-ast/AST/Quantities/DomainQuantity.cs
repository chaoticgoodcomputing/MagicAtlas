namespace MagicAST.AST.Quantities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A quantity equal to the card's <b>domain</b> — the number of basic land types
/// among lands the relevant player controls ("for each basic land type among lands
/// you control"). A specific game-value quantity in the same family as
/// <see cref="DevotionQuantity"/> (CR 700.5 devotion), carried as its own record
/// rather than a <see cref="DerivedKind"/> so no shared enum edit is needed.
///
/// <para>
/// CR 305.6: "The basic land types are Plains, Island, Swamp, Mountain, and Forest.
/// If an object uses the words 'basic land type,' it's referring to one of these
/// subtypes. An object with the land card type and a basic land type has the intrinsic
/// ability '{T}: Add [mana symbol],' even if the text box doesn't actually contain
/// that text or the object has no text box." Domain counts how many of these five
/// distinct basic land types appear among the controlled lands.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the domain reference; the engine
/// counts the distinct basic land types among the controlled lands at evaluation time.
/// It does NOT pre-resolve to a literal 5 (the maximum) or any fixed value. Field-less —
/// mirrors <see cref="DieRollResultQuantity"/> / <see cref="AnyAmountQuantity"/>.
/// Serializes as <c>{"QuantityType":"domain"}</c>.
/// </para>
/// </summary>
[OracleQuantity("domain")]
public sealed record DomainQuantity : Quantity;
