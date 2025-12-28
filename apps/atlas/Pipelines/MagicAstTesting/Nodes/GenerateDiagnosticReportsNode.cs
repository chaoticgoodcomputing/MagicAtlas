using MagicAST;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.Diagnostics;
using MagicAST.Parsing;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Pipelines.MagicAstTesting.Nodes;

/// <summary>
/// Pipeline node that analyzes all cards and generates diagnostic reports.
/// </summary>
public static class GenerateDiagnosticReportsNode
{
  /// <summary>
  /// Configuration for diagnostic report generation.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Maximum number of examples to include per diagnostic. Default: 3.
    /// </summary>
    public int MaxExamplesPerDiagnostic { get; init; } = 3;

    /// <summary>
    /// Maximum number of top diagnostics to include per ability type in reports. Default: 3.
    /// </summary>
    public int TopDiagnosticsPerAbilityType { get; init; } = 3;
  }

  /// <summary>
  /// Creates a transform function that parses all cards and generates error/warning reports.
  /// </summary>
  public static Func<
    IEnumerable<CardInputDTO>,
    Task<(MagicAstParsingReport Errors, MagicAstParsingReport Warnings)>
  > Create(Params? parameters = null)
  {
    var config = parameters ?? new Params();

    return async (inputs) =>
    {
      var inputList = inputs.ToList();
      var totalCards = inputList.Count;

      // Create parser instance
      var parser = new CardParser();

      // Parse all cards and collect full card data
      var parsedCards = new List<(CardInputDTO Input, CardParseResult Result)>();

      foreach (var cardInput in inputList)
      {
        var parseResult = parser.Parse(cardInput);
        parsedCards.Add((cardInput, parseResult));
      }

      // Calculate card-level statistics
      var fullyParsedCards = parsedCards.Count(c =>
        c.Result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error)
      );
      var cardsWithErrors = parsedCards.Count(c =>
        c.Result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
      );
      var partiallyParsedCards = parsedCards.Count(c =>
        c.Result.Output.Oracle.Abilities.Count > 0
        && c.Result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
      );
      var unparsedCards = parsedCards.Count(c =>
        c.Result.Output.Oracle.Abilities.Count == 0
        && c.Result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
      );

      // Collect ability-level statistics by type
      var activatedAbilities = parsedCards
        .SelectMany(c => c.Result.Output.Oracle.Abilities.OfType<ActivatedAbility>())
        .ToList();
      var triggeredAbilities = parsedCards
        .SelectMany(c => c.Result.Output.Oracle.Abilities.OfType<TriggeredAbility>())
        .ToList();
      var staticAbilities = parsedCards
        .SelectMany(c => c.Result.Output.Oracle.Abilities.OfType<StaticAbility>())
        .ToList();
      var spellAbilities = parsedCards
        .SelectMany(c => c.Result.Output.Oracle.Abilities.OfType<SpellAbility>())
        .ToList();

      var totalAbilities =
        activatedAbilities.Count
        + triggeredAbilities.Count
        + staticAbilities.Count
        + spellAbilities.Count;

      // Collect diagnostics by severity and ability type
      var errorDiagnostics = parsedCards
        .SelectMany(c =>
          c.Result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => (Card: c.Input.Name, Diagnostic: d))
        )
        .ToList();

      var warningDiagnostics = parsedCards
        .SelectMany(c =>
          c.Result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning)
            .Select(d => (Card: c.Input.Name, Diagnostic: d))
        )
        .ToList();

      // Categorize errors by ability type
      var errorsByType = CategorizeErrorsByAbilityType(errorDiagnostics, config);
      var warningsByType = CategorizeErrorsByAbilityType(warningDiagnostics, config);

      // Count failed abilities per category from diagnostics
      var failedSpellCount = errorsByType.SpellAbilities.Sum(e => e.Count);
      var failedActivatedCount = errorsByType.ActivatedAbilities.Sum(e => e.Count);
      var failedTriggeredCount = errorsByType.TriggeredAbilities.Sum(e => e.Count);
      var failedStaticCount = errorsByType.StaticAbilities.Sum(e => e.Count);

      var totalFailedAbilities =
        failedSpellCount + failedActivatedCount + failedTriggeredCount + failedStaticCount;

      var errorReport = new MagicAstParsingReport
      {
        CardStatistics = new CardLevelStatistics
        {
          TotalCards = totalCards,
          FullyParsedCards = fullyParsedCards,
          PartiallyParsedCards = partiallyParsedCards,
          UnparsedCards = unparsedCards,
        },
        AbilityStatistics = new AbilityLevelStatistics
        {
          TotalAbilities = totalAbilities + totalFailedAbilities,
          ParsedAbilities = totalAbilities,
          FailedAbilities = totalFailedAbilities,
          ByCategory = new AbilityCategoryBreakdown
          {
            KeywordAbilities = new AbilityCategoryStatistics
            {
              Total = spellAbilities.Count + failedSpellCount,
              Parsed = spellAbilities.Count,
              Failed = failedSpellCount,
            },
            ActivatedAbilities = new AbilityCategoryStatistics
            {
              Total = activatedAbilities.Count + failedActivatedCount,
              Parsed = activatedAbilities.Count,
              Failed = failedActivatedCount,
            },
            TriggeredAbilities = new AbilityCategoryStatistics
            {
              Total = triggeredAbilities.Count + failedTriggeredCount,
              Parsed = triggeredAbilities.Count,
              Failed = failedTriggeredCount,
            },
            StaticAbilities = new AbilityCategoryStatistics
            {
              Total = staticAbilities.Count + failedStaticCount,
              Parsed = staticAbilities.Count,
              Failed = failedStaticCount,
            },
          },
        },
        ErrorsByAbilityType = errorsByType,
      };

      var warningReport = new MagicAstParsingReport
      {
        CardStatistics = new CardLevelStatistics
        {
          TotalCards = totalCards,
          FullyParsedCards = parsedCards.Count(c =>
            c.Result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Warning)
          ),
          PartiallyParsedCards = parsedCards.Count(c =>
            c.Result.Output.Oracle.Abilities.Count > 0
            && c.Result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning)
          ),
          UnparsedCards = parsedCards.Count(c =>
            c.Result.Output.Oracle.Abilities.Count == 0
            && c.Result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning)
          ),
        },
        AbilityStatistics = new AbilityLevelStatistics
        {
          TotalAbilities = totalAbilities,
          ParsedAbilities = totalAbilities,
          FailedAbilities = 0,
          ByCategory = new AbilityCategoryBreakdown
          {
            KeywordAbilities = new AbilityCategoryStatistics
            {
              Total = spellAbilities.Count,
              Parsed = spellAbilities.Count,
              Failed = 0,
            },
            ActivatedAbilities = new AbilityCategoryStatistics
            {
              Total = activatedAbilities.Count,
              Parsed = activatedAbilities.Count,
              Failed = 0,
            },
            TriggeredAbilities = new AbilityCategoryStatistics
            {
              Total = triggeredAbilities.Count,
              Parsed = triggeredAbilities.Count,
              Failed = 0,
            },
            StaticAbilities = new AbilityCategoryStatistics
            {
              Total = staticAbilities.Count,
              Parsed = staticAbilities.Count,
              Failed = 0,
            },
          },
        },
        ErrorsByAbilityType = warningsByType,
      };

      return await Task.FromResult((errorReport, warningReport));
    };
  }

  /// <summary>
  /// Categorizes diagnostics by ability type based on heuristics from the error message.
  /// </summary>
  private static AbilityTypeErrors CategorizeErrorsByAbilityType(
    List<(string Card, Diagnostic Diagnostic)> diagnostics,
    Params config
  )
  {
    var spellErrors =
      new Dictionary<(string? Code, string Message), List<(string CardName, string AbilityText)>>();
    var activatedErrors =
      new Dictionary<(string? Code, string Message), List<(string CardName, string AbilityText)>>();
    var triggeredErrors =
      new Dictionary<(string? Code, string Message), List<(string CardName, string AbilityText)>>();
    var staticErrors =
      new Dictionary<(string? Code, string Message), List<(string CardName, string AbilityText)>>();

    foreach (var (card, diagnostic) in diagnostics)
    {
      var sourceText = diagnostic.RawText ?? "";
      var key = (diagnostic.Pattern, diagnostic.Message);
      var example = (card, sourceText);

      // Categorize based on source text patterns
      if (IsTriggeredAbility(sourceText))
      {
        if (!triggeredErrors.ContainsKey(key))
        {
          triggeredErrors[key] = new List<(string, string)>();
        }
        triggeredErrors[key].Add(example);
      }
      else if (IsActivatedAbility(sourceText))
      {
        if (!activatedErrors.ContainsKey(key))
        {
          activatedErrors[key] = new List<(string, string)>();
        }
        activatedErrors[key].Add(example);
      }
      else if (IsStaticAbility(sourceText))
      {
        if (!staticErrors.ContainsKey(key))
        {
          staticErrors[key] = new List<(string, string)>();
        }
        staticErrors[key].Add(example);
      }
      else
      {
        // Default to spell if unclear
        if (!spellErrors.ContainsKey(key))
        {
          spellErrors[key] = new List<(string, string)>();
        }
        spellErrors[key].Add(example);
      }
    }

    return new AbilityTypeErrors
    {
      SpellAbilities = BuildErrorPatterns(spellErrors, config, diagnostics.Count),
      ActivatedAbilities = BuildErrorPatterns(activatedErrors, config, diagnostics.Count),
      TriggeredAbilities = BuildErrorPatterns(triggeredErrors, config, diagnostics.Count),
      StaticAbilities = BuildErrorPatterns(staticErrors, config, diagnostics.Count),
    };
  }

  private static List<AbilityErrorPattern> BuildErrorPatterns(
    Dictionary<(string? Code, string Message), List<(string CardName, string AbilityText)>> errors,
    Params config,
    int totalDiagnostics
  )
  {
    return errors
      .OrderByDescending(kvp => kvp.Value.Count)
      .Take(config.TopDiagnosticsPerAbilityType)
      .Select(kvp => new AbilityErrorPattern
      {
        Code = kvp.Key.Code ?? "Unknown",
        Message = kvp.Key.Message,
        Count = kvp.Value.Count,
        PercentageOfCategory =
          totalDiagnostics > 0 ? (kvp.Value.Count * 100.0 / totalDiagnostics) : 0.0,
        Examples = kvp
          .Value.Take(config.MaxExamplesPerDiagnostic)
          .Select(e => new AbilityErrorExample
          {
            CardName = e.CardName,
            AbilityText = e.AbilityText,
          })
          .ToList(),
      })
      .ToList();
  }

  private static bool IsTriggeredAbility(string text)
  {
    return text.StartsWith("When ", StringComparison.OrdinalIgnoreCase)
      || text.StartsWith("Whenever ", StringComparison.OrdinalIgnoreCase)
      || text.StartsWith("At ", StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsActivatedAbility(string text)
  {
    // Contains colon but not a triggered ability
    return text.Contains(':') && !IsTriggeredAbility(text) && !text.StartsWith("(");
  }

  private static bool IsStaticAbility(string text)
  {
    // Heuristics for static abilities
    return text.Contains("Enchant ")
      || text.Contains(" enters tapped")
      || text.Contains(" can't ")
      || text.Contains(" doesn't ")
      || text.Contains(" get ")
      || text.Contains("As long as")
      || text.Contains(" has ");
  }
}
