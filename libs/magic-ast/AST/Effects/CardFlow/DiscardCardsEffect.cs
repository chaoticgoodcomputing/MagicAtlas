namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "discard [count] cards"
///
/// <para>
/// The discarding player is <see cref="Player"/>; the optional <see cref="Filter"/>
/// narrows which card(s) may be discarded ("a nonland card"). CR 701.9a: "To discard
/// a card, move it from its owner's hand to that player's graveyard."
/// </para>
///
/// <para>
/// The targeted reveal-choose-discard family (Thoughtseize / Coercion / Thought Erasure
/// — "Target opponent reveals their hand. You choose a nonland card from it. That player
/// discards that card.") is ONE coupled game action, not three independent sentences:
/// the chosen card from the reveal is the very card discarded. It is modeled here as a
/// single effect with three distinct structured axes —
/// <list type="bullet">
///   <item><see cref="Player"/> — the discarder ("that player", i.e. the targeted opponent);</item>
///   <item><see cref="Chooser"/> — who selects the card ("you"), distinct from the discarder;</item>
///   <item><see cref="Filter"/> — which card qualifies ("a nonland card").</item>
/// </list>
/// The preceding hand reveal is the disclosure that makes the choice possible; it is
/// recorded by <see cref="RevealHand"/> rather than as a separate decomposed effect, so
/// the coupling between "what is revealed" and "what is chosen and discarded" is not lost.
/// </para>
/// </summary>
[OracleEffect("discardCards")]
public sealed record DiscardCardsEffect : Effect
{
  public required Quantity Count { get; init; }

  /// <summary>The player who discards — the discarder. For the reveal-choose-discard
  /// family this is the targeted opponent ("that player discards that card").</summary>
  public required ObjectReference Player { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }

  /// <summary>
  /// Who chooses the discarded card, when that choice is made by someone OTHER than the
  /// discarder — the "you choose" axis of the Thoughtseize / Coercion family. Null in the
  /// common case ("target player discards a card", where the discarder chooses), so it is
  /// only emitted when the oracle text names a separate chooser.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Chooser { get; init; }

  /// <summary>
  /// True when the discarder reveals their hand before the card is chosen (the
  /// disclosure precondition of the reveal-choose-discard family). Records that the
  /// choice is made from the open hand; the reveal is part of the same descriptive
  /// instruction, not a separate game action.
  /// </summary>
  public bool RevealHand { get; init; }

  /// <summary>
  /// True if the discard is random.
  /// </summary>
  public bool Random { get; init; }
}
