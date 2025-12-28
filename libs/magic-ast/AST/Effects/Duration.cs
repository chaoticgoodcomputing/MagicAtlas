namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;

/// <summary>
/// How long an effect lasts.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "durationType")]
[JsonDerivedType(typeof(UntilEndOfTurnDuration), "untilEndOfTurn")]
[JsonDerivedType(typeof(UntilYourNextTurnDuration), "untilYourNextTurn")]
[JsonDerivedType(typeof(AsLongAsDuration), "asLongAs")]
[JsonDerivedType(typeof(PermanentDuration), "permanent")]
[JsonDerivedType(typeof(UntilLeavesBattlefieldDuration), "untilLeavesBattlefield")]
[JsonDerivedType(typeof(UntilEndOfCombatDuration), "untilEndOfCombat")]
[JsonDerivedType(typeof(AtBeginningOfNextEndStepDuration), "atBeginningOfNextEndStep")]
public abstract record Duration;

/// <summary>
/// "until end of turn"
/// </summary>
public sealed record UntilEndOfTurnDuration : Duration;

/// <summary>
/// "until your next turn"
/// </summary>
public sealed record UntilYourNextTurnDuration : Duration;

/// <summary>
/// "as long as [condition]"
/// </summary>
public sealed record AsLongAsDuration : Duration
{
  [JsonPropertyName("condition")]
  public required string Condition { get; init; }
}

/// <summary>
/// Effect is permanent (no duration specified).
/// </summary>
public sealed record PermanentDuration : Duration;

/// <summary>
/// "until [object] leaves the battlefield"
/// </summary>
public sealed record UntilLeavesBattlefieldDuration : Duration
{
  [JsonPropertyName("object")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Object { get; init; }
}

/// <summary>
/// "until end of combat"
/// </summary>
public sealed record UntilEndOfCombatDuration : Duration;

/// <summary>
/// "at the beginning of the next end step" - delayed trigger for effects like exile this creature at end step
/// </summary>
public sealed record AtBeginningOfNextEndStepDuration : Duration;
