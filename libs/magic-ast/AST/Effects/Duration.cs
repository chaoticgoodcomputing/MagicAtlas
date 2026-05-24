namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;
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
  public required string Condition { get; init; }
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
