namespace MagicAST.AST.Costs;

using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "pay {MANA} for each [count]" — a mana payment whose total scales with a
/// runtime count rather than being a fixed symbol list. Scent of Brine: "Counter
/// target spell unless its controller pays {1} for each card revealed this way."
/// The per-unit mana (<see cref="PerUnit"/>, "{1}") is multiplied by the number of
/// units named in <see cref="Count"/> (here "card revealed this way", a
/// <see cref="CardsRevealedThisWayQuantity"/>).
///
/// <para>
/// Distinct from a flat <see cref="ManaCost"/> (a fixed <see cref="ManaCost.Symbols"/>
/// list whose total is known up front): the total generic mana here is
/// (per-unit) × (a game-state count) and so is not knowable until the cost is paid.
/// Reference-not-resolution (ADR 0004): MAST records the per-unit cost and the count
/// reference; the engine evaluates the actual total at payment time. Reuses the mana
/// model (<see cref="ManaCost"/>) for the per-unit component and the quantity model
/// (<see cref="Quantity"/>) for the multiplier rather than free-texting "{1} for each
/// card revealed this way".
/// </para>
///
/// <para>
/// CR 118.1 (verbatim): "A cost is an action or payment necessary to take another
/// action or to stop another action from taking place. To pay a cost, a player
/// carries out the instructions specified by the spell, ability, or effect that
/// contains that cost." (The unless-clause payment stops the counter from happening.)
/// </para>
/// </summary>
[OracleCost("scaledMana")]
public sealed record ScaledManaCost : Cost
{
  /// <summary>
  /// The mana paid per unit of <see cref="Count"/> — "{1}" is a single generic
  /// <see cref="ManaSymbol"/> wrapped in a <see cref="ManaCost"/>.
  /// </summary>
  public required ManaCost PerUnit { get; init; }

  /// <summary>
  /// How many units are paid — "for each card revealed this way" is a
  /// <see cref="CardsRevealedThisWayQuantity"/>.
  /// </summary>
  public required Quantity Count { get; init; }
}
