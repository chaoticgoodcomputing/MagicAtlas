namespace MagicAST.AST.References;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;

/// <summary>
/// A reference to an object in the game.
/// e.g., "target creature", "this creature", "it", "you"
/// </summary>
public sealed record ObjectReference
{
  /// <summary>
  /// The kind of reference.
  /// </summary>
  public required ObjectReferenceKind Kind { get; init; }

  /// <summary>
  /// Optional filter describing what objects this refers to.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }

  /// <summary>
  /// Optional cardinality on the reference itself — used for "up to N target",
  /// "N target", "any number of target" phrasings. When null, the reference is
  /// singular (the default for Target/Self/It). Distinct from <see cref="Filter"/>,
  /// which describes *which* objects qualify; this describes *how many* are chosen.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Quantity { get; init; }

  /// <summary>
  /// The alternatives of a <see cref="ObjectReferenceKind.Choice"/> reference —
  /// "that player or a planeswalker that player controls" (Curse of the Pierced
  /// Heart). The chooser selects exactly one of these references at resolution.
  /// Null for every non-Choice kind. The "or" is a single chosen reference, so it
  /// is modeled as one Choice reference rather than two separate targets.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<ObjectReference>? Options { get; init; }

  // Factory methods
  public static ObjectReference Self() => new() { Kind = ObjectReferenceKind.Self };

  public static ObjectReference Target(ObjectFilter filter) =>
    new() { Kind = ObjectReferenceKind.Target, Filter = filter };

  public static ObjectReference It() => new() { Kind = ObjectReferenceKind.It };

  public static ObjectReference You() => new() { Kind = ObjectReferenceKind.You };
}

/// <summary>
/// The kind of object reference.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObjectReferenceKind
{
  /// <summary>"this creature", "this permanent"</summary>
  Self,

  /// <summary>"target creature", "target player"</summary>
  Target,

  /// <summary>"it" - refers to a previously mentioned object</summary>
  It,

  /// <summary>"you" - the controller of the ability</summary>
  You,

  /// <summary>"an opponent", "target opponent"</summary>
  Opponent,

  /// <summary>"each opponent"</summary>
  EachOpponent,

  /// <summary>"each player"</summary>
  EachPlayer,

  /// <summary>"any target" - creature, player, or planeswalker</summary>
  AnyTarget,

  /// <summary>"another creature", "another target"</summary>
  Another,

  /// <summary>"all creatures", "each creature"</summary>
  Each,

  /// <summary>"its controller"</summary>
  Controller,

  /// <summary>"its owner"</summary>
  Owner,

  /// <summary>"the defending player"</summary>
  DefendingPlayer,

  /// <summary>"enchanted creature", "equipped creature"</summary>
  EnchantedOrEquipped,

  /// <summary>"chosen creature" - from a choice earlier</summary>
  Chosen,

  /// <summary>"that player" - the player who triggered an ability or was mentioned earlier</summary>
  ThatPlayer,

  /// <summary>"that creature" / "the blocking creature" — the creature named by the trigger condition (e.g. Flanking's "becomes blocked by a creature without flanking" → the blocking creature). Creature analogue of <see cref="ThatPlayer"/>; refers back to the object the trigger's Filter identified, not the ability's own source (which is <see cref="Self"/>/<see cref="It"/>).</summary>
  ThatCreature,

  /// <summary>"that ability" — the activated or triggered ability on the stack named by an
  /// <see cref="MagicAST.AST.Triggers.TriggerEvent.AbilityActivated"/> trigger (CR 113 — an ability is an
  /// object once on the stack). Ability analogue of <see cref="ThatPlayer"/>/<see cref="ThatCreature"/>;
  /// the copy target for Rings of Brighthearth's "copy that ability". A linked back-reference (ADR 0004),
  /// not a threaded binding.</summary>
  TriggeringAbility,

  /// <summary>"each other player" - all players except the controller</summary>
  EachOtherPlayer,

  /// <summary>A specific named or designated object — e.g. "your commander", "the monarch". Use Filter to describe which.</summary>
  Designated,

  /// <summary>"a [filter] you control" — an indefinite controller-choice reference; controller picks one qualifying permanent at resolution. Not targeted (no "target" keyword in oracle). Distinct from <see cref="Target"/> (Rule 115.1 — only "target" creates a target).</summary>
  Any,

  /// <summary>"both creatures" in a Soulbond paired-grant context (Rule 702.95). Refers to both the creature carrying the soulbond ability and its current pair partner. Only meaningful while the two creatures are paired.</summary>
  BothPaired,

  /// <summary>"[X] or [Y]" — a chooser-selected reference among <see cref="ObjectReference.Options"/> alternatives ("that player or a planeswalker that player controls"). Exactly one option is chosen at resolution.</summary>
  Choice,

  /// <summary>"the creature this card haunts" — the object a Haunt card was exiled haunting (CR 702.55). A linked-ability reference (ADR 0004 reference-not-resolution), not a threaded binding. Paired with <see cref="Effects.ZoneChange.ExileEffect.HauntsTarget"/> on the production side.</summary>
  Haunted,

  /// <summary>"the encoded creature" — the creature a ciphered spell is encoded on (CR 702.99); its combat damage triggers casting a copy. Paired with <see cref="Effects.ZoneChange.ExileEffect.EncodedOn"/> on the production side.</summary>
  Encoded,
}
