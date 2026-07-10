namespace MagicAST.AST.Costs;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Base type for all costs in Magic.
/// Costs are what must be paid to cast spells or activate abilities.
/// </summary>
[PolymorphicBase("CostType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Cost>))]
public abstract record Cost;

/// <summary>
/// A mana cost like "{2}{G}{G}" or "{X}{R}".
/// </summary>
[OracleCost("mana")]
public sealed record ManaCost : Cost
{
  /// <summary>
  /// The mana symbols in this cost.
  /// </summary>
  public required IReadOnlyList<ManaSymbol> Symbols { get; init; }
}

/// <summary>
/// The tap symbol {T}.
/// </summary>
[OracleCost("tap")]
public sealed record TapCost : Cost
{
  /// <summary>
  /// What to tap. Null means tap this permanent.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}

/// <summary>
/// The untap symbol {Q}.
/// </summary>
[OracleCost("untap")]
public sealed record UntapCost : Cost
{
  /// <summary>
  /// What to untap. Null means untap this permanent.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}

/// <summary>
/// "Sacrifice a [filter]" or "Sacrifice X [filter]s".
/// </summary>
[OracleCost("sacrifice")]
public sealed record SacrificeCost : Cost
{
  /// <summary>
  /// What must be sacrificed.
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many must be sacrificed.
  /// </summary>
  public required Quantity Quantity { get; init; }
}

/// <summary>
/// "Discard a card" or "Discard X cards".
/// </summary>
[OracleCost("discard")]
public sealed record DiscardCost : Cost
{
  /// <summary>
  /// What must be discarded.
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many must be discarded.
  /// </summary>
  public required Quantity Quantity { get; init; }

  /// <summary>
  /// True when the card(s) to discard are chosen "at random" rather than by the
  /// discarding player — e.g. Balduvian Horde's "sacrifice it unless you discard a
  /// card at random." CR 701.9b: "By default, effects that cause a player to discard
  /// a card allow the affected player to choose which card to discard. Some effects,
  /// however, require a random discard or allow another player to choose which card
  /// is discarded." This flag records the "random discard" variant. Mirrors
  /// <see cref="MagicAST.AST.Effects.CardFlow.DiscardCardsEffect.Random"/> on the
  /// cost side. Omitted from serialization when false (the common, non-random case)
  /// so existing discard-cost fixtures are unaffected.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool Random { get; init; }
}

/// <summary>
/// "Reveal a [color] card in your hand" — a non-mana cost, most commonly the alternative
/// "turn face up" cost variant of Morph (CR 702.37a): "Morph—Reveal a red card in your
/// hand" rather than a mana morph cost. Distinct from the general
/// <see cref="MagicAST.AST.Effects.CardFlow.RevealCardsEffect"/> (an effect that reveals
/// cards as part of resolving a spell/ability): this is the COST paid, modelled with the
/// same Filter/Quantity/Zone shape as <see cref="DiscardCost"/> and <see cref="ExileCost"/>.
/// </summary>
[OracleCost("reveal")]
public sealed record RevealCost : Cost
{
  /// <summary>
  /// Which card(s) qualify to be revealed — "a red card" is <c>CardTypes=["card"]</c> +
  /// <c>Colors=["R"]</c>.
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many cards must be revealed — "a … card" is a single card.
  /// </summary>
  public required Quantity Quantity { get; init; }

  /// <summary>
  /// The zone the revealed card(s) come from — <see cref="Zone.Hand"/> for "in your hand".
  /// </summary>
  public required Zone Zone { get; init; }
}

/// <summary>
/// "Pay N life".
/// </summary>
[OracleCost("payLife")]
public sealed record PayLifeCost : Cost
{
  /// <summary>
  /// How much life to pay.
  /// </summary>
  public required Quantity Amount { get; init; }
}

/// <summary>
/// "Exile [filter] from [zone]".
/// </summary>
[OracleCost("exile")]
public sealed record ExileCost : Cost
{
  /// <summary>
  /// What must be exiled.
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many must be exiled.
  /// </summary>
  public required Quantity Quantity { get; init; }

  /// <summary>
  /// The zone to exile from.
  /// </summary>
  public required Zone FromZone { get; init; }
}

/// <summary>
/// "Remove N counters from [target]".
/// </summary>
[OracleCost("removeCounters")]
public sealed record RemoveCountersCost : Cost
{
  /// <summary>
  /// The type of counter to remove.
  /// </summary>
  public required string CounterType { get; init; }

  /// <summary>
  /// How many counters to remove.
  /// </summary>
  public required Quantity Quantity { get; init; }

  /// <summary>
  /// What to remove counters from.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}

/// <summary>
/// "Tap N untapped [filter]s you control".
/// </summary>
[OracleCost("tapPermanents")]
public sealed record TapPermanentsCost : Cost
{
  /// <summary>
  /// What must be tapped.
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many must be tapped.
  /// </summary>
  public required Quantity Quantity { get; init; }
}

/// <summary>
/// Multiple costs combined with commas.
/// e.g., "{2}{B}, {T}, Sacrifice a creature"
/// </summary>
[OracleCost("composite")]
public sealed record CompositeCost : Cost
{
  /// <summary>
  /// The individual costs that make up this composite cost.
  /// </summary>
  public required IReadOnlyList<Cost> Costs { get; init; }
}

/// <summary>
/// "Return this [permanent] to its owner's hand" activation cost (a self-bounce) — e.g. Grinning Ignus
/// "{R}, Return this creature to its owner's hand: Add {C}{C}{R}". CR 701.x: the permanent is moved to
/// the Hand zone as part of paying the cost, so the ability can't repeat without recasting it.
/// </summary>
[OracleCost("returnToHand")]
public sealed record ReturnToHandCost : Cost
{
  /// <summary>What returns. For a self-bounce cost this is <see cref="ObjectReference.Self"/>.</summary>
  public required ObjectReference Target { get; init; }
}

/// <summary>
/// "Forage" keyword action cost (CR 701.61a): "Exile three cards from your graveyard
/// or sacrifice a Food." Printed as a bare keyword on the cost side of an activated
/// ability, e.g. "{2}, Forage: [effect]".
///
/// <para>
/// CR 701.61a (verbatim): "To forage means 'Exile three cards from your graveyard
/// or sacrifice a Food.'" MAST records the keyword-action invocation here; the two
/// alternative payment modes (exile-3 or sacrifice-Food) are engine territory —
/// the node names the action, not the execution (per the descriptive-not-engine doctrine).
/// This mirrors how other keyword-action costs are modelled (e.g. Saddle).
/// </para>
/// </summary>
[OracleCost("forage")]
public sealed record ForageCost : Cost;

/// <summary>
/// "Pay N {E}" — pay N energy counters as an activation cost. Rule 107.14: The energy
/// symbol is {E}; each represents one energy counter. "Pay eight {E}" removes eight
/// energy counters from the activating player. MAST records the verb + count; the
/// player-state bookkeeping (removing the counters) is engine territory.
/// </summary>
[OracleCost("payEnergy")]
public sealed record PayEnergyCost : Cost
{
  /// <summary>How many energy counters ({E}) must be paid.</summary>
  public required Quantity Amount { get; init; }
}

/// <summary>
/// "Put N [type] counter(s) on [target]" — a cost that places counters as part of
/// activating an ability. Used by cards like Devoted Druid:
/// "Put a -1/-1 counter on this creature: Untap this creature."
///
/// <para>
/// CR 122.1: "A counter is a marker placed on an object or player that modifies its
/// characteristics and/or interacts with a rule, ability, or effect."
/// CR 602.1a: "The activation cost is everything before the colon (:)."
/// The placement of the counter is itself the cost; the engine resolves the counter
/// placement at activation time.
/// </para>
/// </summary>
[OracleCost("putCounters")]
public sealed record PutCountersCost : Cost
{
  /// <summary>
  /// The type of counter to place (e.g. "-1/-1", "+1/+1", "charge").
  /// </summary>
  public required string CounterType { get; init; }

  /// <summary>
  /// How many counters to place.
  /// </summary>
  public required Quantity Quantity { get; init; }

  /// <summary>
  /// What to place the counters on. Null means place on this permanent.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}

/// <summary>
/// "Unattach [card name]" — the activation cost of undetaching a specifically named
/// Equipment from the creature it is currently attached to. Unlike the parameterless
/// <see cref="MagicAST.AST.Effects.Modification.UnattachEffect"/> (the effect of
/// unattaching), this is a cost paid as part of activating an ability (KHM Toralf's
/// Hammer: "{1}{R}, {T}, Unattach Toralf's Hammer: It deals 3 damage to any target.
/// Return Toralf's Hammer to its owner's hand."). The named Equipment must be detached
/// from the equipped creature before the ability resolves.
///
/// <para>
/// Rule 201.4: "If an object's oracle text refers to the object it's applying to by
/// name, that reference applies to any object with that name." The card name
/// <see cref="CardName"/> is retained verbatim from oracle text; the engine resolves
/// which physical permanent is being referred to.
/// </para>
/// </summary>
[OracleCost("unattachNamed")]
public sealed record UnattachNamedCost : Cost
{
  /// <summary>
  /// The printed name of the Equipment to unattach, verbatim from oracle text
  /// (e.g. "Toralf's Hammer"). CR 201.4 — self-reference by card name.
  /// </summary>
  public required string CardName { get; init; }
}
