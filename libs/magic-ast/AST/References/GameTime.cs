namespace MagicAST.AST.References;

using System.Text.Json.Serialization;

/// <summary>
/// A point on the game's turn timeline (CR 500-series turn structure) — the
/// shared primitive composed by the three rules objects that reference the clock:
/// time-triggers ("At the beginning of your upkeep"), delayed triggers ("…at the
/// beginning of the next end step"), and duration endpoints ("until end of turn").
/// A value type, not an effect or ability. See ADR 0002.
/// </summary>
public sealed record GameTime
{
  /// <summary>The phase/step (or coarse Turn/Combat) this point sits in.</summary>
  public required TurnPart Part { get; init; }

  /// <summary>Which edge of <see cref="Part"/> — its beginning or its end.</summary>
  public required TimeBoundary Edge { get; init; }

  /// <summary>"this" vs "next" occurrence. Null means this (the default).</summary>
  public TimeRelation? When { get; init; }

  /// <summary>Whose turn/step, when the oracle text qualifies it ("your next turn"). Null when unqualified.</summary>
  public ControllerFilter? Whose { get; init; }
}

/// <summary>A phase or step of the turn, plus the coarse whole-turn and whole-combat spans.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TurnPart>))]
public enum TurnPart
{
  Upkeep,
  Draw,
  PrecombatMain,
  Combat,
  PostcombatMain,
  End,
  Cleanup,
  Turn,
}

/// <summary>The beginning or end edge of a <see cref="TurnPart"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TimeBoundary>))]
public enum TimeBoundary
{
  Beginning,
  End,
}

/// <summary>"this" or "next" occurrence of the referenced point.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TimeRelation>))]
public enum TimeRelation
{
  This,
  Next,
}
