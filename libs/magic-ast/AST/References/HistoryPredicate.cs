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
/// "[object] dealt damage by [source] [timeframe]". e.g.
/// "a creature dealt damage by Zurgo this turn".
/// </summary>
[HistoryPredicateKind("dealtDamageBy")]
public sealed record DealtDamageByPredicate : HistoryPredicate
{
  /// <summary>
  /// The source that dealt the damage.
  /// </summary>
  public required ObjectReference Source { get; init; }

  /// <summary>
  /// Free-text timeframe descriptor — "this turn", "since your last turn",
  /// "this combat", etc. Structured timeframes can be added later.
  /// </summary>
  [FreeTextField]
  public required string Timeframe { get; init; }
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
