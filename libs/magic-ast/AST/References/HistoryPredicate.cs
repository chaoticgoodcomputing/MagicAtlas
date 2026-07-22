namespace MagicAST.AST.References;

using System.Text.Json.Serialization;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Base type for backward-looking lifecycle predicates that restrict an
/// <see cref="ObjectFilter"/>. Where the rest of <see cref="ObjectFilter"/>
/// describes the object's *current* characteristics, a history predicate
/// describes *something that happened* to or with the object during a window
/// of past time — e.g. "a creature dealt damage by X this turn", "a spell you
/// cast this turn", "a permanent that entered the battlefield this turn".
///
/// <para>
/// These predicates are descriptive only — MAST records what the oracle text
/// says happened, not how a runtime engine would track it.
/// </para>
/// </summary>
[PolymorphicBase("PredicateType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<HistoryPredicate>))]
public abstract record HistoryPredicate;

/// <summary>
/// "[object] dealt damage by [source] [window]". e.g.
/// "a creature dealt damage by Zurgo this turn" or
/// "a creature dealt damage this way".
/// </summary>
[HistoryPredicateKind("dealtDamageBy")]
public sealed record DealtDamageByPredicate : HistoryPredicate
{
  /// <summary>
  /// The source that dealt the damage.
  /// </summary>
  public required ObjectReference Source { get; init; }

  /// <summary>
  /// Which damage-events the predicate looks back over — see <see cref="DamageWindow"/>.
  /// Structured (was free text): the two observed surfaces "this turn" (a turn-scoped
  /// temporal window) and "this way" (a CR 607.1 linked-ability provenance link) are
  /// distinct CR concepts, so this is a <see cref="DamageWindow"/> enum, not a string.
  /// </summary>
  public required DamageWindow Window { get; init; }
}

/// <summary>
/// The scope over which a <see cref="DealtDamageByPredicate"/> looks back for the damage
/// it references. NOT a pure timeframe: the two members are two different CR concepts that
/// both happen to restrict "dealt damage" — one temporal, one provenance-linked.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DamageWindow
{
  /// <summary>
  /// "this turn" — a turn-scoped temporal window: the object was dealt damage at some
  /// point during the current turn (CR 514 / the turn structure bounds "this turn" to the
  /// current turn). e.g. Sengir Bats, "a creature dealt damage by this creature this turn".
  /// </summary>
  ThisTurn,

  /// <summary>
  /// "this way" — a CR 607.1 linked-ability provenance link, NOT a temporal window: the
  /// object was dealt damage by the resolution of this very spell/ability, which the phrase
  /// "this way" points back to. e.g. Incendiary Flow, "If a creature dealt damage this way
  /// would die this turn, exile it instead" (the "this turn" there bounds the death event,
  /// not this predicate).
  /// </summary>
  ThisWay,
}

/// <summary>
/// "[creature] that crewed [vehicle] [timeframe]". e.g.
/// "target nonlegendary creature that crewed it this turn".
/// </summary>
[HistoryPredicateKind("crewed")]
public sealed record CrewedPredicate : HistoryPredicate
{
  /// <summary>
  /// The vehicle that was crewed.
  /// </summary>
  public required ObjectReference Vehicle { get; init; }

  /// <summary>
  /// Free-text timeframe descriptor.
  /// </summary>
  [FreeTextField]
  public required string Timeframe { get; init; }
}

/// <summary>
/// "cast by [caster] this turn" — a spell was placed on the stack by the
/// named controller during the current turn. Used to count spells in a
/// <see cref="CountQuantity"/> for effects like Aetherflux Reservoir's
/// "you gain 1 life for each spell you've cast this turn" (CR 601 — casting
/// a spell places it on the stack; "this turn" bounds the window to the
/// current turn's actions). The Caster axis identifies who cast them;
/// usually <see cref="ControllerFilter.You"/> but may be Any for storm-like
/// shapes.
/// </summary>
[HistoryPredicateKind("castThisTurn")]
public sealed record CastThisTurnPredicate : HistoryPredicate
{
  /// <summary>
  /// Who cast the spells being counted.
  /// </summary>
  public required ControllerFilter Caster { get; init; }

  /// <summary>
  /// Optional ordinal qualifier selecting which occurrence in the turn's cast
  /// sequence this predicate names — e.g. "the first creature spell you cast
  /// each turn" → <c>Ordinal = 1</c> (Shadow in the Warp's cost-reduction static
  /// ability). Mirrors <see cref="MagicAST.AST.Triggers.TriggerCondition.Ordinal"/>'s
  /// descriptive-only convention: MAST records which occurrence the oracle text
  /// names, leaving the per-turn tally and reset to the engine. Null means every
  /// matching spell counts (the default, unordered usage for storm/Aetherflux-style
  /// <see cref="MagicAST.AST.Quantities.CountQuantity"/> counts).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? Ordinal { get; init; }
}

/// <summary>
/// "cards put into [player's] graveyard from [zone] this turn" — a backward-
/// looking count predicate for the Fraying Sanity family (CR 701.17 mill;
/// "At the beginning of each end step, enchanted player mills X cards, where X
/// is the number of cards put into their graveyard from anywhere this turn").
/// Counts objects that were moved into the named player's graveyard during the
/// current turn window.
///
/// <para>
/// CR 406.6 (graveyard zone); CR 400.1 (zones); "from anywhere" in oracle text
/// encodes <c>Zone.Anywhere</c> on <see cref="FromZone"/> (null means unqualified,
/// which is any zone by default but distinct from the explicit "from anywhere"
/// qualifier).
/// </para>
/// </summary>
[HistoryPredicateKind("putIntoGraveyardThisTurn")]
public sealed record PutIntoGraveyardThisTurnPredicate : HistoryPredicate
{
  /// <summary>
  /// The source zone restriction — which zone the cards must have come from.
  /// Null when the oracle text does not qualify the source zone.
  /// <c>Zone.Anywhere</c> for the explicit "from anywhere" qualifier on Fraying Sanity.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Zone? FromZone { get; init; }
}

/// <summary>
/// Escape hatch for backward-looking predicates that don't yet have a
/// structured shape — carries only the literal oracle phrase. Use sparingly;
/// prefer a structured concrete predicate when the shape recurs.
/// </summary>
[HistoryPredicateKind("other")]
public sealed record OtherHistoryPredicate : HistoryPredicate, IResidual
{
  /// <summary>
  /// The literal predicate phrase from the oracle text.
  /// </summary>
  public required string Description { get; init; }
}

/// <summary>
/// "who lost life this turn" — a backward-looking predicate restricting a
/// PLAYER filter to those who lost life during the current turn window (CR
/// 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly."). Gev, Scaled Scorch: "Other
/// creatures you control enter with an additional +1/+1 counter on them for
/// each opponent who lost life this turn."
///
/// <para>
/// A marker (no fields): WHICH players are checked (controller vs. opponent)
/// is carried on the enclosing <see cref="ObjectFilter.Controller"/>/
/// <see cref="ObjectFilter.EntityType"/> axes, not duplicated here — mirrors
/// <see cref="MagicAST.AST.Abilities.PrecedingActionPerformedCondition"/>'s
/// field-less convention for a fixed idiom with no further parameters. The
/// identical "opponent lost life this turn" surface already names the
/// BOOLEAN precondition of Spectacle (CR 702.137a,
/// <see cref="MagicAST.Keywords.Definitions.SpectacleKeyword"/>); this
/// predicate is the COUNTING sibling — used inside a
/// <see cref="MagicAST.AST.Quantities.CountQuantity"/> over a player-scoped
/// <see cref="ObjectFilter"/> rather than a yes/no cast-cost gate.
/// </para>
/// </summary>
[HistoryPredicateKind("lostLifeThisTurn")]
public sealed record LostLifeThisTurnPredicate : HistoryPredicate;

/// <summary>
/// "that attacked this turn" — a backward-looking predicate restricting a filter to
/// objects that were declared as attackers during the current turn window (CR 508.1:
/// declaring attackers; the "this turn" window bounds it to the current turn per the
/// turn structure). Rowdy Research: "This spell costs {1} less to cast for each creature
/// that attacked this turn." — the count scales with creatures that attacked this turn.
///
/// <para>
/// A marker (no fields), mirroring <see cref="LostLifeThisTurnPredicate"/>'s field-less
/// convention: WHICH objects are checked (creature, controller/owner side) is carried on
/// the enclosing <see cref="ObjectFilter"/> axes, not duplicated here. Descriptive only —
/// MAST records that the oracle text names an attacked-this-turn history, leaving the
/// per-turn tracking to the engine (descriptive-not-engine doctrine).
/// </para>
/// </summary>
[HistoryPredicateKind("attackedThisTurn")]
public sealed record AttackedThisTurnPredicate : HistoryPredicate;
