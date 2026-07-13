namespace MagicAST.AST.Quantities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A quantity equal to the total amount of mana spent to cast the spell that became
/// this permanent — "equal to the amount of mana spent to cast it" (Gyrus, Waker of
/// Corpses: "Gyrus enters with a number of +1/+1 counters on it equal to the amount
/// of mana spent to cast it.").
///
/// <para>
/// CR 601.2f (verbatim): "The player determines the total cost of the spell. Usually
/// this is just the mana cost. ... The total cost is the mana cost or alternative
/// cost (as determined in rule 601.2b), plus all additional costs and cost increases,
/// and minus all cost reductions." CR 601.2h (verbatim): "The player pays the total
/// cost. First, they pay all costs that don't involve random elements or moving
/// objects from the library to a public zone, in any order. Then they pay all
/// remaining costs in any order. Partial payments are not allowed. Unpayable costs
/// can't be paid." — the amount of mana actually spent (e.g. generic mana paid for
/// an <c>{X}</c> cost) is a fact fixed once the spell finishes being cast, exactly
/// like the color-keyed <see cref="MagicAST.AST.Abilities.ManaSpentToCastCondition"/>
/// but reading the total NUMBER of mana spent rather than a boolean color check.
/// </para>
///
/// <para>
/// Distinct from <see cref="DerivedKind.ManaValue"/> (a <see cref="DerivedQuantity"/>
/// reading the spell's PRINTED mana value, a static characteristic): the amount of
/// mana spent is the dynamic total actually paid at cast time, which may exceed the
/// printed mana value when a variable cost (<c>{X}</c>) is involved — Gyrus's own
/// <c>{X}{B}{R}{G}</c> cost is the canonical case. Also distinct from Sunburst
/// (CR 702.44, GLOSSARY), which counts the number of DISTINCT COLORS of mana spent
/// (a per-color tally, left as engine territory / keyword presence per the
/// descriptive-not-engine doctrine) rather than the total numeric amount.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): the engine reads the actual total mana spent
/// to cast the spell that became this permanent; MAST does not pre-evaluate it.
/// Field-less, mirroring <see cref="DomainQuantity"/> / <see cref="DieRollResultQuantity"/>.
/// Serializes as <c>{"QuantityType":"manaSpentToCast"}</c>.
/// </para>
/// </summary>
[OracleQuantity("manaSpentToCast")]
public sealed record ManaSpentToCastQuantity : Quantity;
