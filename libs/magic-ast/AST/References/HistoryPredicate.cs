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
