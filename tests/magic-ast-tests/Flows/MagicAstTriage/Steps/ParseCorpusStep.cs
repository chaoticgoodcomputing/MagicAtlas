using Flowthru.Step;
using MagicAST;
using MagicAST.AST.Abilities;
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
    var parsedAbilities = abilities.Count(a => a is not UnparsedAbility);

    var unparsed = abilities.OfType<UnparsedAbility>().ToList();
    var lines = AttributeLines(oracleText, unparsed);

    return new ParseRecord
    {
      ScryfallId = input.ScryfallId,
      CardName = input.Input.Name,
      Input = input.Input,
      TotalAbilities = totalAbilities,
      ParsedAbilities = parsedAbilities,
      Lines = lines,
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
    IReadOnlyList<UnparsedAbility> unparsed
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

      foreach (var ua in unparsed)
      {
        var spanStart = ua.SourceSpan.Start;
        var spanEnd = ua.SourceSpan.Start + ua.SourceSpan.Length;
        // Half-open overlap: the span touches this line iff it starts before the
        // line ends AND ends after the line starts.
        if (spanStart < end && spanEnd > start)
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
