namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "there are N or more card types among cards in [your|a] graveyard" — the
/// Delirium mechanic's activation gate (CR 207.2c: Delirium is an ability word with
/// no special rules meaning; the condition is the engine-evaluated predicate).
///
/// <para>
/// Distinct from <see cref="CountCondition"/>: a <c>CountCondition</c> counts the
/// NUMBER of objects matching a filter; this condition counts the number of DISTINCT
/// CARD TYPES among a set of objects. A graveyard with seven cards but all Instants
/// satisfies a CountCondition of seven or more but fails this condition's diversity
/// test if the threshold is &gt;= 4.
/// </para>
///
/// <para>
/// CR 205.2 lists the card types: artifact, battle, conspiracy, creature, dungeon,
/// enchantment, instant, kindred, land, phenomenon, plane, planeswalker, scheme,
/// sorcery, vanguard. In practice Delirium counts the creature/artifact/enchantment/
/// instant/sorcery/land/planeswalker subset that appears in a player's graveyard.
/// </para>
///
/// <para>
/// <b>Example:</b>
/// Shifting Woodland — "Activate only if there are four or more card types among cards
/// in your graveyard." → Count: { Operator: GreaterThanOrEqual, Value: 4 },
/// Zone: Graveyard, Owner: You.
/// </para>
/// </summary>
[ConditionKind("cardTypeDiversity")]
public sealed record CardTypeDiversityCondition : Condition
{
  /// <summary>
  /// The threshold comparison for the distinct-type count — e.g.
  /// { Operator: GreaterThanOrEqual, Value: 4 } for "four or more card types".
  /// </summary>
  public required Comparison Count { get; init; }

  /// <summary>
  /// The zone in which the cards are counted (Graveyard for Delirium).
  /// </summary>
  public required Zone Zone { get; init; }

  /// <summary>
  /// Who owns the zone — <see cref="ControllerFilter.You"/> for "your graveyard",
  /// null for "a graveyard" (any graveyard).
  /// </summary>
  public ControllerFilter? Owner { get; init; }
}
