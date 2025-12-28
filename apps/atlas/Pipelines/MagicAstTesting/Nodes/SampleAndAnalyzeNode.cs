using MagicAST;
using MagicAST.Diagnostics;
using MagicAST.Parsing;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Pipelines.MagicAstTesting.Nodes;

/// <summary>
/// Pipeline node that randomly samples cards and generates AST analysis with diagnostics.
/// </summary>
public static class SampleAndAnalyzeNode
{
  /// <summary>
  /// Configuration for sampling and analysis.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of cards to randomly sample. Default: 10.
    /// </summary>
    public int SampleSize { get; init; } = 4;

    /// <summary>
    /// Random seed for reproducibility. If null, uses random seed.
    /// </summary>
    public int? RandomSeed { get; init; }
  }

  /// <summary>
  /// Creates a transform function that samples cards and performs AST analysis.
  /// </summary>
  public static Func<IEnumerable<CardInputDTO>, Task<IEnumerable<MagicAstCardAnalysis>>> Create(
    Params? parameters = null
  )
  {
    var config = parameters ?? new Params();

    return async (inputs) =>
    {
      // Convert to list for sampling
      var inputList = inputs.ToList();

      // Initialize random number generator
      var random = config.RandomSeed.HasValue ? new Random(config.RandomSeed.Value) : new Random();

      // Sample cards randomly
      var sampleSize = Math.Min(config.SampleSize, inputList.Count);
      var sampledCards = inputList.OrderBy(_ => random.Next()).Take(sampleSize).ToList();

      // Create parser instance
      var parser = new CardParser();

      // Parse each sampled card and create analysis output
      var outputs = sampledCards.Select(cardInput =>
      {
        // Parse the card using MagicAST library
        var parseResult = parser.Parse(cardInput);

        // Determine if parsing succeeded (no errors)
        var hasErrors = parseResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

        // Create the analysis output
        return new MagicAstCardAnalysis
        {
          Name = cardInput.Name,
          Ast = parseResult.Output,
          Diagnostics = parseResult.Diagnostics.ToList(),
          ParseSucceeded = !hasErrors,
        };
      });

      return await Task.FromResult(outputs);
    };
  }
}
