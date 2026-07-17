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
      foreach (var ability in clauseAbilities)
      {
        abilities.Add(StampProvenance(ability, oracleText, clause.SourceSpan));
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
  /// Recursively stamps oracle-text provenance (<see cref="Ability.SourceSpan"/> +
  /// <see cref="Ability.OracleLineIndex"/>) onto an ability AND its nested container bodies — Class
  /// base/level abilities, Saga chapter bodies, Modal option abilities. Top-level clauses carry their own
  /// clause span; a nested body carries the sub-clause span its container parser attached (falling back to
  /// the parent's span), and the line index is computed from that span's start. Without the recursion a
  /// nested body defaults to line 0 / the parent's span — the mis-attribution the span-witness triage
  /// surfaced (a Class level, Saga chapter, or Siege mode landing on the wrong oracle line). Provenance
  /// only: the nested body's parse (effects/triggers) is untouched.
  /// </summary>
  private Ability StampProvenance(Ability ability, string oracleText, TextSpan fallback)
  {
    var span = ability.SourceSpan ?? fallback;
    ability = ability with
    {
      SourceSpan = span,
      OracleLineIndex = OracleLineIndexFor(oracleText, span.Start),
    };
    return ability switch
    {
      ClassAbility c => c with
      {
        BaseAbilities = c.BaseAbilities.Select(a => StampProvenance(a, oracleText, span)).ToList(),
        Levels = c
          .Levels.Select(l =>
            l with
            {
              Abilities = l.Abilities.Select(a => StampProvenance(a, oracleText, span)).ToList(),
            }
          )
          .ToList(),
      },
      SagaAbility s => s with
      {
        Chapters = s
          .Chapters.Select(ch => ch with { Body = StampProvenance(ch.Body, oracleText, span) })
          .ToList(),
      },
      ModalAbility m => m with
      {
        Modes = m
          .Modes.Select(o => o with { Ability = StampProvenance(o.Ability, oracleText, span) })
          .ToList(),
      },
      _ => ability,
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
