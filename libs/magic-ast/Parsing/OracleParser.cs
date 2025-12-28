namespace MagicAST.Parsing;

using System.Diagnostics;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.Diagnostics;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;

/// <summary>
/// Main orchestrator for parsing Magic: The Gathering oracle text.
/// Coordinates the tokenizer, clause splitter, classifier, and individual parsers.
/// </summary>
public sealed class OracleParser
{
  private readonly ClauseSplitter _splitter = new();
  private readonly AbilityClassifier _classifier = new();
  private readonly TriggeredAbilityParser _triggeredParser = new();
  private readonly ActivatedAbilityParser _activatedParser = new();
  private readonly StaticAbilityParser _staticParser = new();
  private readonly FallbackParser _fallbackParser = new();

  /// <summary>
  /// Parses oracle text into a structured CardOracle AST.
  /// </summary>
  /// <param name="oracleText">The oracle text to parse.</param>
  /// <returns>A ParseResult containing the AST and diagnostics.</returns>
  public ParseResult Parse(string? oracleText)
  {
    var stopwatch = Stopwatch.StartNew();
    var diagnostics = new List<Diagnostic>();

    // Handle null/empty oracle text
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      stopwatch.Stop();
      return new ParseResult
      {
        Output = new CardOracle { RawText = oracleText ?? string.Empty, Abilities = [] },
        Status = ParseStatus.FullyParsed,
        Diagnostics = [],
        Metrics = new ParseMetrics
        {
          TotalAbilities = 0,
          ParsedAbilities = 0,
          FailedAbilities = 0,
          DurationMs = stopwatch.Elapsed.TotalMilliseconds,
        },
      };
    }

    // Split into clauses
    var clauses = _splitter.Split(oracleText);

    // Parse each clause
    var abilities = new List<Ability>();
    var parsedCount = 0;
    var failedCount = 0;

    foreach (var clause in clauses)
    {
      var (clauseAbilities, clauseDiagnostics) = ParseClause(clause);
      abilities.AddRange(clauseAbilities);
      diagnostics.AddRange(clauseDiagnostics);

      // Count parsed vs failed abilities
      foreach (var ability in clauseAbilities)
      {
        if (ability is UnparsedAbility)
        {
          failedCount++;
        }
        else
        {
          parsedCount++;
        }
      }
    }

    stopwatch.Stop();

    // Determine overall status
    var status = DetermineStatus(parsedCount, failedCount);

    return new ParseResult
    {
      Output = new CardOracle { RawText = oracleText, Abilities = abilities },
      Status = status,
      Diagnostics = diagnostics,
      Metrics = new ParseMetrics
      {
        TotalAbilities = clauses.Count,
        ParsedAbilities = parsedCount,
        FailedAbilities = failedCount,
        DurationMs = stopwatch.Elapsed.TotalMilliseconds,
      },
    };
  }

  /// <summary>
  /// Parses a single clause into one or more abilities.
  /// Some clauses (like comma-separated keywords) expand into multiple abilities.
  /// </summary>
  private (IReadOnlyList<Ability> Abilities, IReadOnlyList<Diagnostic> Diagnostics) ParseClause(
    OracleClause clause
  )
  {
    // Classify the clause
    var classification = _classifier.Classify(clause);

    // Route to appropriate parser based on classification
    var abilities = classification.Kind switch
    {
      AbilityKind.Triggered => ParseTriggeredAbilityClause(clause, classification),
      AbilityKind.Activated => ParseActivatedAbilityClause(clause, classification),
      AbilityKind.Static => ParseStaticAbilityClause(clause, classification),
      AbilityKind.Modal => ParseModalAbilityClause(clause, classification),
      AbilityKind.Spell => ParseSpellAbilityClause(clause, classification),
      _ => new[] { _fallbackParser.Parse(clause, classification) },
    };

    // Collect diagnostics from UnparsedAbility nodes
    var diagnostics = abilities
      .OfType<UnparsedAbility>()
      .SelectMany(unparsed => unparsed.Diagnostics)
      .ToList();

    return (abilities, diagnostics);
  }

  /// <summary>
  /// Parses a triggered ability clause.
  /// Returns a single-element array with the ability.
  /// </summary>
  private IReadOnlyList<Ability> ParseTriggeredAbilityClause(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    // Try the TriggeredAbilityParser first
    var parsed = _triggeredParser.TryParse(clause, classification);
    if (parsed != null)
    {
      return new[] { parsed };
    }

    // Fall back to unparsed if parsing failed
    return new[]
    {
      _fallbackParser.Parse(clause, classification, "Triggered ability parser not yet implemented"),
    };
  }

  /// <summary>
  /// Parses an activated ability clause.
  /// Returns a single-element array with the ability.
  /// </summary>
  private IReadOnlyList<Ability> ParseActivatedAbilityClause(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    // Try the ActivatedAbilityParser first
    var parsed = _activatedParser.TryParse(clause, classification);
    if (parsed != null)
    {
      return new[] { parsed };
    }

    // Fall back to unparsed if parsing failed
    return new[]
    {
      _fallbackParser.Parse(clause, classification, "Activated ability parser not yet implemented"),
    };
  }

  /// <summary>
  /// Parses a static ability clause.
  /// May return multiple abilities if the clause contains comma-separated keywords.
  /// </summary>
  private IReadOnlyList<Ability> ParseStaticAbilityClause(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    // Try the StaticAbilityParser first
    var parsed = _staticParser.TryParse(clause, classification);
    if (parsed != null && parsed.Count > 0)
    {
      return parsed;
    }

    // Fall back to unparsed if parsing failed
    return new[]
    {
      _fallbackParser.Parse(clause, classification, "Static ability parser not yet implemented"),
    };
  }

  /// <summary>
  /// Parses a modal ability clause.
  /// Returns a single-element array with the ability.
  /// </summary>
  private IReadOnlyList<Ability> ParseModalAbilityClause(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    // TODO: Implement ModalAbilityParser
    // For now, use fallback with informative message
    return new[]
    {
      _fallbackParser.Parse(clause, classification, "Modal ability parser not yet implemented"),
    };
  }

  /// <summary>
  /// Parses a spell ability clause.
  /// Returns a single-element array with the ability.
  /// </summary>
  private IReadOnlyList<Ability> ParseSpellAbilityClause(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    // TODO: Implement SpellAbilityParser
    // For now, use fallback with informative message
    return new[]
    {
      _fallbackParser.Parse(clause, classification, "Spell ability parser not yet implemented"),
    };
  }

  /// <summary>
  /// Determines the overall parse status based on success/failure counts.
  /// </summary>
  private static ParseStatus DetermineStatus(int parsedCount, int failedCount)
  {
    if (failedCount == 0)
    {
      return ParseStatus.FullyParsed;
    }

    if (parsedCount == 0)
    {
      return ParseStatus.Failed;
    }

    return ParseStatus.Partial;
  }
}
