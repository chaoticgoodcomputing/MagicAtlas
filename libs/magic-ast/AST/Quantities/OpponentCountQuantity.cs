namespace MagicAST.AST.Quantities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A quantity equal to the number of opponents the ability's controller has —
/// "for each opponent you have" (Blazing Sunsteel: "Equipped creature gets +1/+0
/// for each opponent you have."). A specific game-value quantity in the same
/// field-less family as <see cref="DomainQuantity"/> / <see cref="DieRollResultQuantity"/>
/// / <see cref="AnyAmountQuantity"/>, carried as its own record rather than a
/// <see cref="DerivedKind"/> so no shared enum edit is needed.
///
/// <para>
/// This is DISTINCT from the game-state-filtered "opponent" sets already
/// carried as free-text <see cref="CalculatedQuantity.Expression"/> residuals
/// elsewhere (e.g. "for each opponent you attacked with a creature this combat"
/// on Melee, "for each opponent dealt damage" on Malcolm) — those count a
/// SUBSET of opponents matching a combat/damage predicate that is outside
/// <see cref="AST.References.ObjectFilter"/> scope. "Opponents you have" has no
/// such predicate: it is literally the count of players who are the
/// controller's opponents, a single well-defined rules concept with no
/// sub-filter, so a dedicated field-less node is the honest structural shape
/// (an <see cref="AST.References.ObjectFilter"/>-based <see cref="CountQuantity"/>
/// would require overloading <see cref="AST.References.ObjectFilter.Controller"/>
/// — a "who controls this object" axis — onto player entities that have no
/// controller of their own, which is not a faithful reuse).
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the opponent-count
/// reference; the engine counts the players who are opponents of the ability's
/// controller at evaluation time (1 in a two-player game, more in multiplayer).
/// It does NOT pre-resolve to a literal 1. Field-less; serializes as
/// <c>{"QuantityType":"opponentCount"}</c>.
/// </para>
/// </summary>
[OracleQuantity("opponentCount")]
public sealed record OpponentCountQuantity : Quantity;
