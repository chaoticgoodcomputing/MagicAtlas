namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "exile [target]"
/// </summary>
[OracleEffect("exile")]
public sealed record ExileEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// "exile [target] with [N] [type] counters on it" — counters placed on
  /// the card as part of the exile action (suspend-like patterns).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public CounterPlacement? WithCounters { get; init; }

  /// <summary>
  /// "exile [this] haunting [target]" — Haunt (CR 702.55): when this creature dies
  /// (or this card is put into a graveyard from the battlefield), it is exiled
  /// haunting a creature, and a linked ability triggers when the haunted creature
  /// dies. Parallel to <see cref="WithCounters"/>: the exile gains this structure only
  /// when the card prints the haunt link. Referenced elsewhere via
  /// <see cref="ObjectReferenceKind.Haunted"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? HauntsTarget { get; init; }

  /// <summary>
  /// "exile [this spell] encoded on [a creature]" — Cipher (CR 702.99): "you may exile
  /// this card encoded on a creature you control", and that creature's combat damage
  /// lets you cast a copy. Referenced elsewhere via
  /// <see cref="ObjectReferenceKind.Encoded"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? EncodedOn { get; init; }

  /// <summary>
  /// "exile [target] face down" — the card is placed in exile hidden from
  /// opponents (CR 406.3: "Cards 'exiled face down' can't be examined by any
  /// player except when instructions allow it"). The exiling player may look at
  /// it per CR 406.3. Used by Ugin, the Ineffable's +1 ability and similar effects.
  /// Null (omitted in JSON) for normal face-up exile.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsFaceDown { get; init; }
}
