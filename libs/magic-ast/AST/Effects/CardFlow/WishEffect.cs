namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may reveal a [filter] card you own from outside the game or choose a
/// face-up [filter] card you own in exile. Put that card into your hand." —
/// the Karn, the Great Creator −2 / Glittering Wish / Mastermind's Acquisition
/// family. The controller retrieves a card matching <see cref="CardFilter"/>
/// from either outside the game (sideboard) or exile and puts it into their hand.
///
/// <para>
/// CR 400.11b: "Some effects bring cards into a game from outside the game. Those
/// cards remain in the game until the game ends, their owner leaves the game, or a
/// rule or effect removes them from the game, whichever comes first." The two source
/// zones (outside game and exile) are stated explicitly on the card; this node
/// records both as the <see cref="Sources"/> list. MAST records what the oracle
/// text says (reference-not-resolution, ADR 0004) — which sources are available
/// and which filter qualifies the card — not the runtime evaluation.
/// </para>
///
/// <para>
/// The "You may" optionality is encoded by an <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/>
/// wrapper around this node at the parse level (ADR 0005: clause-level modifiers
/// are composition). The <see cref="WishSource"/> enum records the legal retrieval
/// zones so a consumer can distinguish the broad form (outside game + exile) from
/// a narrower form (outside game only, or exile only).
/// </para>
///
/// <para>
/// "Face-up" qualifies the exile-zone source: only face-up exiled cards may be
/// chosen (Karn can't retrieve face-down morphs or other hidden cards). This is
/// captured by <see cref="FaceUpOnly"/> on the exile branch.
/// </para>
/// </summary>
[OracleEffect("wish")]
public sealed record WishEffect : Effect
{
  /// <summary>
  /// The card-type / owner filter that limits which card the controller may retrieve
  /// — e.g. <c>CardTypes=["artifact"], Owner=You</c> for "an artifact card you own".
  /// </summary>
  public required ObjectFilter CardFilter { get; init; }

  /// <summary>
  /// Which retrieval sources are available (outside the game, exile, or both).
  /// Karn's −2 offers both; narrower Wish variants may offer only one.
  /// </summary>
  public required IReadOnlyList<WishSource> Sources { get; init; }

  /// <summary>
  /// Whether only face-up cards in the exile zone may be chosen — true for Karn's
  /// "face-up artifact card … in exile" clause. Null / false when no face-up
  /// restriction is stated (e.g. outside-the-game cards are always visible).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? FaceUpOnly { get; init; }
}

/// <summary>
/// A legal retrieval zone for a <see cref="WishEffect"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WishSource
{
  /// <summary>"from outside the game" — the player's sideboard (CR 400.11a).</summary>
  [JsonStringEnumMemberName("outsideGame")]
  OutsideGame,

  /// <summary>"in exile" — the exile zone (CR 406).</summary>
  [JsonStringEnumMemberName("exile")]
  Exile,
}
