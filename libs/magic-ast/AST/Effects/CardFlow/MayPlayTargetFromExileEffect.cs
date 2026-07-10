namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may play that card from exile [this turn]." — a one-shot permission to
/// play a SPECIFIC, previously-referenced card while it sits in exile, bounded
/// by a stated duration (Norin, Swift Survivalist: "Whenever a creature you
/// control becomes blocked, you may exile it. You may play that card from
/// exile this turn.").
///
/// <para>
/// Distinct from <see cref="MayPlayFromExileEffect"/> (a continuous static
/// permission over a FILTERED set of cards, keyed by <see cref="ObjectFilter.ExiledWith"/>
/// — CR 406.6 "reference not resolution" topology) and from
/// <see cref="MayCastTargetFromGraveyardEffect"/> (the graveyard analogue of THIS
/// node — same one-shot/Target shape, different zone and verb). Here the
/// referenced object is a single card named by pronoun back-reference (the "it"
/// exiled by a sibling effect earlier in the SAME ability), not a filter — so
/// <see cref="Target"/> carries an <see cref="ObjectReferenceKind.It"/> reference
/// rather than a zone filter (ADR 0004 "reference not resolution").
/// </para>
///
/// <para>
/// CR 701.13a (exile) places the card in the exile zone; this effect is the
/// separate permission grant that lets the controller play it from there
/// (CR 305.1 "play a land"/601.2 "cast a spell" normally require hand). The
/// stated window ("this turn") is carried on the inherited
/// <see cref="ContinuousEffect.Duration"/> (<c>UntilTimeDuration.EndOfTurn</c>),
/// matching the identical "you may play that card this turn" surface already
/// modelled with an end-of-turn <c>Duration</c> on <see cref="ImpulseEffect"/>
/// (Count on Luck). MAST describes the permission; the engine handles the
/// legality check, zone-change, and stack placement (ADR 0003/0004
/// describe-not-execute).
/// </para>
/// </summary>
[OracleEffect("mayPlayTargetFromExile")]
public sealed record MayPlayTargetFromExileEffect : ContinuousEffect
{
  /// <summary>
  /// The card the controller may play from exile — a back-reference
  /// (typically <see cref="ObjectReferenceKind.It"/>) to the object a sibling
  /// effect in the same ability just exiled.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}
