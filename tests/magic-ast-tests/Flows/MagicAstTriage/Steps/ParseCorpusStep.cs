using Flowthru.Step;
using MagicAST;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.Analysis;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;

/// <summary>
/// Runs the MagicAST <see cref="OracleParser"/> over each card's FULL oracle
/// text in a single parse — the same entry point the real parser uses — so
/// <c>ClauseSplitter</c> groups multi-line constructs (modal "Choose one —" +
/// bullets, saga preamble + chapters, level-up stanzas) exactly as they parse
/// in production. Per-line diagnostics are then derived by attributing each
/// <c>UnparsedAbility</c> back to the oracle line(s) its <c>SourceSpan</c>
/// covers.
/// </summary>
/// <remarks>
/// Replaces the prior per-line parse loop, which measured a strawman: it parsed
/// each newline-bounded line in isolation, so a modal header or saga chapter
/// showed as unparsed even when the whole card parsed end-to-end. The full-card
/// parse also tends to be faster (ClauseSplitter runs once per card, not once
/// per line). Serial single-pass; the parser is logically pure, so a future
/// move to <c>AsParallel</c> stays drop-in.
/// </remarks>
[FlowthruStep]
public static class ParseCorpusStep
{
  public static Func<IEnumerable<MastCardInput>, IEnumerable<ParseRecord>> Create() =>
    inputs =>
    {
      var parser = new OracleParser();
      return inputs.Select(input => ParseOne(parser, input)).ToList();
    };

  private static ParseRecord ParseOne(OracleParser parser, MastCardInput input)
  {
    var oracleText = ResolveOracleText(input.Input);

    if (string.IsNullOrEmpty(oracleText))
    {
      return new ParseRecord
      {
        ScryfallId = input.ScryfallId,
        CardName = input.Input.Name,
        Input = input.Input,
        TotalAbilities = 0,
        ParsedAbilities = 0,
        Lines = new List<LineOutcome>(),
        Residuals = new List<ResidualKindCount>(),
      };
    }

    // Single full-card parse.
    var result = parser.Parse(oracleText);
    var abilities = result.Output.Abilities;
    var totalAbilities = abilities.Count;
    // An ability counts as parsed only if it contains NO unparsed node at any
    // depth: a nested UnparsedEffect is still a parse failure (ADR 0001 goal a),
    // so an ability hiding one no longer passes as fully parsed.
    var parsedAbilities = abilities.Count(a => ResidualWalker.CollectUnparsed(a).Count == 0);

    var unparsedNodes = ResidualWalker.CollectUnparsed(result.Output);
    var lines = AttributeLines(oracleText, unparsedNodes);

    // Fidelity level (worst across the whole card): any IUnparsed hole → L0;
    // else any IResidual (deferred interior / free-text) → L1; else fully
    // structured → L2. This is the honest coverage axis — it separates the
    // residual-carrying cards the legacy "no IUnparsed" test counted as fully
    // parsed (they are L1, not L2).
    var debt = ResidualWalker.Analyze(result.Output);
    var fidelityLevel = debt.Unparsed.Count > 0 ? 0 : (debt.Residuals.Count > 0 ? 1 : 2);

    // Lossy-but-clean detection: a trigger deficit means a clause collapsed
    // structure without emitting an UnparsedAbility (the per-line diagnostics
    // can't see it). Only meaningful when the card ISN'T already visibly failing
    // on that content — but computing it unconditionally is harmless and lets the
    // exemplar ranking de-prioritise these risky "clean-looking" cards.
    var lossy = LossyParseAnalyzer.Analyze(oracleText, abilities);

    return new ParseRecord
    {
      ScryfallId = input.ScryfallId,
      CardName = input.Input.Name,
      Input = input.Input,
      TotalAbilities = totalAbilities,
      ParsedAbilities = parsedAbilities,
      FidelityLevel = fidelityLevel,
      Lines = lines,
      SuspectedLossy = lossy.SuspectedLossy,
      DroppedTriggers = lossy.DroppedTriggers,
      Residuals = result
        .Metrics.ResidualCounts.Select(kv => new ResidualKindCount { Kind = kv.Key, Count = kv.Value })
        .ToList(),
    };
  }

  /// <summary>
  /// For multi-faced cards, concatenate face oracle texts with double newlines so
  /// the per-face line indices stay separable; for single-faced cards, just use
  /// the card-level oracle text.
  /// </summary>
  private static string ResolveOracleText(CardInputDTO card)
  {
    if (!string.IsNullOrWhiteSpace(card.OracleText))
    {
      return card.OracleText;
    }
    if (card.CardFaces is not null && card.CardFaces.Count > 0)
    {
      return string.Join(
        "\n\n",
        card.CardFaces.Select(f => f.OracleText ?? string.Empty).Where(t => t.Length > 0)
      );
    }
    return string.Empty;
  }

  /// <summary>
  /// Splits the raw oracle text into non-empty lines (matching the prior
  /// newline-bounded unit of analysis) and attributes each unparsed ability to
  /// every line its <see cref="UnparsedAbility.SourceSpan"/> overlaps.
  /// </summary>
  /// <remarks>
  /// Line ranges are computed on the RAW text (offsets include the stripped
  /// <c>\n</c>) so they align with the parser's spans, which are full-text
  /// offsets assigned by <c>ClauseSplitter</c>. <see cref="LineOutcome.LineIndex"/>
  /// counts only non-empty lines, preserving the prior indexing semantics.
  /// </remarks>
  private static IReadOnlyList<LineOutcome> AttributeLines(
    string oracleText,
    IReadOnlyList<IUnparsed> unparsed
  )
  {
    var outcomes = new List<LineOutcome>();
    var displayIndex = 0;
    var offset = 0;

    foreach (var segment in oracleText.Split('\n'))
    {
      var start = offset;
      var end = offset + segment.Length; // [start, end) in raw coordinates
      offset = end + 1; // advance past the '\n'

      var trimmed = segment.Trim();
      if (trimmed.Length == 0)
      {
        continue;
      }

      var patterns = new List<string>();
      var diagnostics = new List<LineDiagnostic>();

      foreach (var node in unparsed)
      {
        var spanStart = node.SourceSpan.Start;
        var spanEnd = node.SourceSpan.Start + node.SourceSpan.Length;
        // Half-open overlap: the span touches this line iff it starts before the
        // line ends AND ends after the line starts.
        if (spanStart >= end || spanEnd <= start)
        {
          continue;
        }

        if (node is UnparsedAbility ua && ua.Diagnostics.Count > 0)
        {
          foreach (var d in ua.Diagnostics)
          {
            var pattern = d.Pattern ?? "Unknown";
            patterns.Add(pattern);
            diagnostics.Add(
              new LineDiagnostic
              {
                Pattern = pattern,
                LastAttemptedRule = d.LastAttemptedRule,
                FailurePosition = d.FailurePosition,
              }
            );
          }
        }
        else
        {
          // Nested failures (UnparsedEffect) and any IUnparsed without diagnostics:
          // synthesize a coarse pattern from the node type. The lexical-template
          // clusterer breaks these into buildable families by oracle text — the
          // pattern is only the "where it fails" annotation.
          var pattern = node.GetType().Name;
          patterns.Add(pattern);
          diagnostics.Add(
            new LineDiagnostic
            {
              Pattern = pattern,
              LastAttemptedRule = null,
              FailurePosition = node.SourceSpan.Start,
            }
          );
        }
      }

      outcomes.Add(
        new LineOutcome
        {
          LineIndex = displayIndex,
          OracleLine = trimmed,
          Patterns = patterns,
          Diagnostics = diagnostics,
        }
      );
      displayIndex++;
    }

    return outcomes;
  }
}
