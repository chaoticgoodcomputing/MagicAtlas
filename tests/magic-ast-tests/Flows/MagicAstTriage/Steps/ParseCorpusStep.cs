using Flowthru.Step;
using MagicAST;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;

/// <summary>
/// Runs the MagicAST <see cref="OracleParser"/> over each card's oracle text,
/// one newline-bounded line at a time. The unit of analysis is the oracle
/// line — matching what <c>OracleLineTests</c> uses — so each line's
/// diagnostic patterns can be aggregated independently.
/// </summary>
/// <remarks>
/// Serial single-pass (Q4 = (a)) — measure first, parallelise if too slow.
/// The parser is allocation-heavy but logically pure, so a future move to
/// <c>AsParallel</c> should be drop-in.
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
    // Oracle lines = newline-split chunks of the (possibly-face-aggregated) oracle text.
    var oracleText = ResolveOracleText(input.Input);

    var lines = string.IsNullOrEmpty(oracleText)
      ? new List<LineOutcome>()
      : SplitLines(oracleText)
        .Select((text, idx) => ParseLine(parser, idx, text))
        .ToList();

    return new ParseRecord
    {
      ScryfallId = input.ScryfallId,
      CardName = input.Input.Name,
      Input = input.Input,
      Lines = lines,
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

  private static IEnumerable<string> SplitLines(string oracleText) =>
    oracleText
      .Split('\n')
      .Select(line => line.Trim())
      .Where(line => line.Length > 0);

  private static LineOutcome ParseLine(OracleParser parser, int index, string lineText)
  {
    var result = parser.Parse(lineText);
    var patterns = result
      .Output.Abilities.OfType<UnparsedAbility>()
      .SelectMany(unparsed => unparsed.Diagnostics)
      .Select(d => d.Pattern ?? "Unknown")
      .ToList();

    return new LineOutcome
    {
      LineIndex = index,
      OracleLine = lineText,
      Patterns = patterns,
    };
  }
}
