namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// How long an effect lasts.
/// </summary>
[PolymorphicBase("DurationType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Duration>))]
public abstract record Duration;

/// <summary>
/// "as long as [condition]"
/// </summary>
[OracleDuration("asLongAs")]
public sealed record AsLongAsDuration : Duration
{
  public required Condition Condition { get; init; }
}

/// <summary>
/// Effect is permanent (no duration specified).
/// </summary>
[OracleDuration("permanent")]
public sealed record PermanentDuration : Duration;

/// <summary>
/// "until [object] leaves the battlefield"
/// </summary>
[OracleDuration("untilLeavesBattlefield")]
public sealed record UntilLeavesBattlefieldDuration : Duration
{
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Object { get; init; }
}

/// <summary>
/// "until [a clock point]" — the single canonical representation of a continuous
/// effect's expiry at a point on the turn timeline (ADR 0002): "until end of turn"
/// → { Part: Turn, Edge: End }, "until end of combat" → { Part: Combat, Edge: End },
/// "until your next turn" → { Part: Turn, Edge: Beginning, When: Next, Whose: You }.
/// There is no flat per-point variant — every clock-bounded duration is a
/// <see cref="GameTime"/>. (The former "at the beginning of the next …" durations
/// were never durations: they are delayed triggered abilities, CR 603.7, now in
/// <see cref="MagicAST.AST.Abilities.DelayedTriggeredAbility"/>.)
/// </summary>
[OracleDuration("untilTime")]
public sealed record UntilTimeDuration : Duration
{
  public required GameTime Until { get; init; }

  /// <summary>"until end of turn" — the most common clock-bounded duration.</summary>
  public static UntilTimeDuration EndOfTurn =>
    new() { Until = new GameTime { Part = TurnPart.Turn, Edge = TimeBoundary.End } };

  /// <summary>"until end of combat".</summary>
  public static UntilTimeDuration EndOfCombat =>
    new() { Until = new GameTime { Part = TurnPart.Combat, Edge = TimeBoundary.End } };

  /// <summary>"until your next turn" — the beginning of the controller's next turn.</summary>
  public static UntilTimeDuration YourNextTurn =>
    new() { Until = new GameTime { Part = TurnPart.Turn, Edge = TimeBoundary.Beginning, When = TimeRelation.Next, Whose = ControllerFilter.You } };
}
