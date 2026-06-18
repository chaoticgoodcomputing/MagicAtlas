namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "put [a/an] [filter] card from your hand onto the battlefield [tapped [and attacking [that opponent]]]"
///
/// <para>
/// Zone-change action: a card is moved directly from the controller's hand to the
/// battlefield without being cast. The card may enter tapped and/or enter as an
/// attacking creature already declared as attacking the opponent who triggered the
/// ability. Kaalia of the Vast is the canonical instance:
/// "put an Angel, Demon, or Dragon creature card from your hand onto the battlefield
/// tapped and attacking that opponent."
/// </para>
///
/// <para>
/// This effect is always optional at the Kaalia pattern site — the "you may" wrapper
/// is expressed via <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/> in the
/// enclosing effect list, not by a flag here (ADR 0005 clause-modifier composition).
/// </para>
///
/// <para>
/// CR 400.7 (zone-change creates a new object); CR 508.1b (attacks an opponent —
/// the attack target is the same opponent the trigger named); CR 110.5b
/// (entering tapped); CR 508 (entering as an attacking creature in the Declare
/// Attackers Step).
/// </para>
///
/// <para>
/// Distinct from <see cref="ReturnToBattlefieldEffect"/> (moves from graveyard or
/// exile to battlefield) and <see cref="MagicAST.AST.Effects.CardFlow.TopLookPutOntoBattlefieldEffect"/>
/// (top-of-library look + put). The source zone here is always the controller's hand.
/// </para>
/// </summary>
[OracleEffect("putFromHandOntoBattlefield")]
public sealed record PutFromHandOntoBattlefieldEffect : Effect
{
  /// <summary>
  /// Restricts which cards may be chosen from hand.
  /// e.g. Angel, Demon, or Dragon creature card → Subtypes:[Angel, Demon, Dragon], CardTypes:[creature]
  /// Zone is implied as Hand (this effect always moves from hand); Zone on the filter
  /// is set to <see cref="Zone.Hand"/> so consumers have an explicit axis to query.
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// True when the card enters the battlefield tapped (CR 110.5b).
  /// Oracle text: "onto the battlefield tapped".
  /// </summary>
  public bool Tapped { get; init; }

  /// <summary>
  /// True when the card enters the battlefield already attacking the opponent named by
  /// the enclosing trigger (the "that opponent" back-reference, CR 508.1b).
  /// Oracle text: "tapped and attacking that opponent".
  /// Null / false when no attacking qualifier appears.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? AttackingThatOpponent { get; init; }
}
