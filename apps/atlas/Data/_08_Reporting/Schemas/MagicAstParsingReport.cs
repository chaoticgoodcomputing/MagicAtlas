using Flowthru.Abstractions;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Comprehensive report on MagicAST parsing coverage and errors.
/// Answers both card-level and ability-level parsing questions.
/// </summary>
public record MagicAstParsingReport
  : ITextSerializable,
    IStructuredSerializable,
    INestedSerializable
{
  /// <summary>
  /// Summary statistics at the card level.
  /// </summary>
  public required CardLevelStatistics CardStatistics { get; init; }

  /// <summary>
  /// Summary statistics at the ability level, broken down by ability type.
  /// </summary>
  public required AbilityLevelStatistics AbilityStatistics { get; init; }

  /// <summary>
  /// Detailed error patterns for each ability type.
  /// </summary>
  public required AbilityTypeErrors ErrorsByAbilityType { get; init; }
}

/// <summary>
/// Card-level parsing statistics.
/// </summary>
public record CardLevelStatistics
{
  /// <summary>
  /// Total number of cards analyzed.
  /// </summary>
  public required int TotalCards { get; init; }

  /// <summary>
  /// Cards with all abilities successfully parsed and no oracle text errors.
  /// </summary>
  public required int FullyParsedCards { get; init; }

  /// <summary>
  /// Cards with at least one ability parsed, but also some errors.
  /// </summary>
  public required int PartiallyParsedCards { get; init; }

  /// <summary>
  /// Cards where no oracle text abilities were successfully parsed.
  /// Card metadata (name, mana cost, types) may still be present.
  /// </summary>
  public required int UnparsedCards { get; init; }

  /// <summary>
  /// Percentage of cards fully parsed (0-100).
  /// </summary>
  public double FullyParsedPercentage =>
    TotalCards > 0 ? (FullyParsedCards * 100.0 / TotalCards) : 0.0;

  /// <summary>
  /// Percentage of cards at least partially parsed (0-100).
  /// </summary>
  public double PartiallyParsedPercentage =>
    TotalCards > 0 ? ((FullyParsedCards + PartiallyParsedCards) * 100.0 / TotalCards) : 0.0;
}

/// <summary>
/// Ability-level parsing statistics, categorized by ability type.
/// </summary>
public record AbilityLevelStatistics
{
  /// <summary>
  /// Total number of ability instances detected across all cards.
  /// </summary>
  public required int TotalAbilities { get; init; }

  /// <summary>
  /// Total abilities successfully parsed.
  /// </summary>
  public required int ParsedAbilities { get; init; }

  /// <summary>
  /// Total abilities that failed to parse.
  /// </summary>
  public required int FailedAbilities { get; init; }

  /// <summary>
  /// Overall ability parsing success rate (0-100).
  /// </summary>
  public double SuccessRate =>
    TotalAbilities > 0 ? (ParsedAbilities * 100.0 / TotalAbilities) : 0.0;

  /// <summary>
  /// Breakdown by ability category.
  /// </summary>
  public required AbilityCategoryBreakdown ByCategory { get; init; }
}

/// <summary>
/// Statistics for each ability category.
/// </summary>
public record AbilityCategoryBreakdown
{
  /// <summary>
  /// Keyword abilities (Flying, Haste, Equip, etc.)
  /// </summary>
  public required AbilityCategoryStatistics KeywordAbilities { get; init; }

  /// <summary>
  /// Activated abilities (costs : effects)
  /// </summary>
  public required AbilityCategoryStatistics ActivatedAbilities { get; init; }

  /// <summary>
  /// Triggered abilities (When/Whenever/At)
  /// </summary>
  public required AbilityCategoryStatistics TriggeredAbilities { get; init; }

  /// <summary>
  /// Static abilities (continuous effects, restrictions, permissions)
  /// </summary>
  public required AbilityCategoryStatistics StaticAbilities { get; init; }
}

/// <summary>
/// Parsing statistics for a single ability category.
/// </summary>
public record AbilityCategoryStatistics
{
  /// <summary>
  /// Total instances of this ability type detected.
  /// </summary>
  public required int Total { get; init; }

  /// <summary>
  /// Successfully parsed instances.
  /// </summary>
  public required int Parsed { get; init; }

  /// <summary>
  /// Failed parsing attempts.
  /// </summary>
  public required int Failed { get; init; }

  /// <summary>
  /// Success rate for this category (0-100).
  /// </summary>
  public double SuccessRate => Total > 0 ? (Parsed * 100.0 / Total) : 0.0;
}

/// <summary>
/// Detailed error patterns for each ability type.
/// </summary>
public record AbilityTypeErrors
{
  /// <summary>
  /// Errors specific to spell abilities.
  /// </summary>
  public required List<AbilityErrorPattern> SpellAbilities { get; init; }

  /// <summary>
  /// Errors specific to activated abilities.
  /// </summary>
  public required List<AbilityErrorPattern> ActivatedAbilities { get; init; }

  /// <summary>
  /// Errors specific to triggered abilities.
  /// </summary>
  public required List<AbilityErrorPattern> TriggeredAbilities { get; init; }

  /// <summary>
  /// Errors specific to static abilities.
  /// </summary>
  public required List<AbilityErrorPattern> StaticAbilities { get; init; }
}

/// <summary>
/// A specific error pattern within an ability category.
/// </summary>
public record AbilityErrorPattern
{
  /// <summary>
  /// Diagnostic error code (e.g., MAST2001, MAST2999).
  /// </summary>
  public required string Code { get; init; }

  /// <summary>
  /// Human-readable error message.
  /// </summary>
  public required string Message { get; init; }

  /// <summary>
  /// Number of abilities with this error pattern.
  /// </summary>
  public required int Count { get; init; }

  /// <summary>
  /// Percentage of this category affected by this error (0-100).
  /// </summary>
  public required double PercentageOfCategory { get; init; }

  /// <summary>
  /// Representative examples of this error.
  /// </summary>
  public required List<AbilityErrorExample> Examples { get; init; }
}

/// <summary>
/// Example of a specific ability parsing error.
/// </summary>
public record AbilityErrorExample
{
  /// <summary>
  /// Card name containing this ability.
  /// </summary>
  public required string CardName { get; init; }

  /// <summary>
  /// The ability text that failed to parse.
  /// </summary>
  public required string AbilityText { get; init; }
}
