namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// How long an effect lasts.
/// </summary>
[PolymorphicBase("DurationType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Duration>))]
public abstract record Duration;

/// <summary>
/// "until end of turn"
/// </summary>
[OracleDuration("untilEndOfTurn")]
public sealed record UntilEndOfTurnDuration : Duration;

/// <summary>
/// "until your next turn"
/// </summary>
[OracleDuration("untilYourNextTurn")]
public sealed record UntilYourNextTurnDuration : Duration;

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
/// "until end of combat"
/// </summary>
[OracleDuration("untilEndOfCombat")]
public sealed record UntilEndOfCombatDuration : Duration;

/// <summary>
/// "at the beginning of the next end step" - delayed trigger for effects like exile this creature at end step
/// </summary>
[OracleDuration("atBeginningOfNextEndStep")]
public sealed record AtBeginningOfNextEndStepDuration : Duration;

/// <summary>
/// "at the beginning of the next turn's upkeep" — delayed draw shape (e.g. Jolt, Ray of Erasure).
/// Rule 313.1: upkeep step is the first step of the beginning phase.
/// </summary>
[OracleDuration("atBeginningOfNextUpkeep")]
public sealed record AtBeginningOfNextUpkeepDuration : Duration;

/// <summary>
/// "at end of combat" — delayed removal timing (e.g. Phantom creature counter-removal pattern).
/// The effect is scheduled to occur at the end of combat step (Rule 511 — End of Combat Step).
/// Distinct from "until end of combat" (<see cref="UntilEndOfCombatDuration"/>), which marks
/// a continuous effect's expiry point; this marks a one-shot event's scheduled trigger time.
/// </summary>
[OracleDuration("atEndOfCombat")]
public sealed record AtEndOfCombatDuration : Duration;

/// <summary>
/// "at the beginning of the next cleanup step" — delayed sacrifice trigger for
/// the cast-as-flash-with-consequence pattern (Armor of Thorns, Rule 702.8e).
/// Rule 514.1: the cleanup step is the final step of the ending phase. MAST
/// records only that the consequence fires at that step boundary; enforcement
/// is engine territory.
/// </summary>
[OracleDuration("atBeginningOfNextCleanupStep")]
public sealed record AtBeginningOfNextCleanupStepDuration : Duration;
