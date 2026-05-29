namespace MagicAST.Parsing;

using System.Diagnostics;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.Analysis;
using MagicAST.Diagnostics;
using MagicAST.Parsing.Tokens;

/// <summary>
/// Main orchestrator for parsing Magic: The Gathering oracle text.
/// Coordinates the tokenizer, clause splitter, classifier, and the
/// reflection-discovered registry of <see cref="IAbilityParser"/> implementations.
/// </summary>
public sealed class OracleParser
{
  private readonly ClauseSplitter _splitter = new();
  private readonly AbilityClassifier _classifier = new();
  private readonly AbilityParserRegistry _parsers = new();

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
          ResidualCounts = new Dictionary<string, int>(),
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

    var output = new CardOracle { RawText = oracleText, Abilities = abilities };

    return new ParseResult
    {
      Output = output,
      Status = status,
      Diagnostics = diagnostics,
      Metrics = new ParseMetrics
      {
        TotalAbilities = clauses.Count,
        ParsedAbilities = parsedCount,
        FailedAbilities = failedCount,
        DurationMs = stopwatch.Elapsed.TotalMilliseconds,
        ResidualCounts = ResidualWalker.Count(output),
      },
    };
  }

  /// <summary>
  /// Parses a single clause into one or more abilities by dispatching to the
  /// kind-specific <see cref="IAbilityParser"/> registered in
  /// <see cref="AbilityParserRegistry"/>.
  /// </summary>
  private (IReadOnlyList<Ability> Abilities, IReadOnlyList<Diagnostic> Diagnostics) ParseClause(
    OracleClause clause
  )
  {
    var classification = _classifier.Classify(clause);
    var abilities = _parsers.GetParser(classification.Kind).Parse(clause, classification);

    var diagnostics = abilities
      .OfType<UnparsedAbility>()
      .SelectMany(unparsed => unparsed.Diagnostics)
      .ToList();

    return (abilities, diagnostics);
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
