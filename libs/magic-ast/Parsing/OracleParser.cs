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

      // Oracle-text provenance (upstream-atlas-data-plan §4): stamp each top-level
      // ability with the originating clause's span + oracle line index so downstream
      // consumers (port projection → Explorer span highlighting) can trace a port
      // back to the exact substring that produced it. Non-destructive: a parser that
      // already attributed a base span keeps it; UnparsedAbility's own serialized
      // span is untouched (the `with` binds the base, in-memory SourceSpan). These
      // fields are [JsonIgnore], so this does not perturb the parser gold fixtures.
      var lineIndex = OracleLineIndexFor(oracleText, clause.SourceSpan.Start);
      foreach (var ability in clauseAbilities)
      {
        var stamped = ability with
        {
          SourceSpan = ability.SourceSpan ?? clause.SourceSpan,
          OracleLineIndex = lineIndex,
        };
        abilities.Add(stamped);
      }
      diagnostics.AddRange(clauseDiagnostics);

      // Count parsed vs failed abilities. An ability is a failure if it IS an
      // UnparsedAbility OR contains any nested IUnparsed node (e.g. an
      // UnparsedEffect): a buried parse failure must not let the ability — or the
      // card — report as fully parsed (ADR 0001 goal a).
      foreach (var ability in clauseAbilities)
      {
        if (ResidualWalker.CollectUnparsed(ability).Count > 0)
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
  /// The 0-based oracle-text line index a clause starts on: the number of newline
  /// characters in <paramref name="oracleText"/> before <paramref name="offset"/>.
  /// Clause spans are offsets into the original (newline-preserving) oracle text,
  /// so counting preceding newlines yields the paragraph/line the ability came from.
  /// </summary>
  private static int OracleLineIndexFor(string oracleText, int offset)
  {
    var bound = Math.Clamp(offset, 0, oracleText.Length);
    var lines = 0;
    for (var i = 0; i < bound; i++)
    {
      if (oracleText[i] == '\n')
      {
        lines++;
      }
    }
    return lines;
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
