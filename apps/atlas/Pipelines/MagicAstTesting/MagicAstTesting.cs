using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.MagicAstTesting.Nodes;

namespace MagicAtlas.Pipelines.MagicAstTesting;

/// <summary>
/// Pipeline for testing MagicAST parsing capabilities.
/// Maps CardCoreData to CardInputDto, samples cards, and generates AST analysis.
/// </summary>
public static class MagicAstTesting
{
  /// <summary>
  /// Configuration parameters for the MagicAST testing pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of cards to randomly sample for AST analysis. Default: 10.
    /// </summary>
    public int SampleSize { get; init; } = 10;

    /// <summary>
    /// Random seed for reproducibility. If null, uses random seed.
    /// </summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// Maximum number of examples to include per diagnostic in reports. Default: 3.
    /// </summary>
    public int MaxExamplesPerDiagnostic { get; init; } = 3;

    /// <summary>
    /// Maximum number of top diagnostics to include per ability type. Default: 3.
    /// </summary>
    public int TopDiagnosticsPerAbilityType { get; init; } = 3;
  }

  /// <summary>
  /// Creates the MagicAST testing pipeline.
  /// </summary>
  public static Pipeline Create(Catalog catalog, Params parameters)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Map CardCoreData to CardInputDto
      pipeline.AddNode(
        label: "MapToCardInput",
        description: "Maps CardCoreData to CardInputDto, extracting only AST-relevant fields",
        transform: MapToCardInputNode.Create(),
        input: catalog.FilteredCardCoreData,
        output: catalog.MagicAstCardInputs
      );

      // Node 2: Sample and analyze cards
      pipeline.AddNode(
        label: "SampleAndAnalyze",
        description: "Randomly samples cards and generates AST analysis with diagnostics",
        transform: SampleAndAnalyzeNode.Create(
          new SampleAndAnalyzeNode.Params
          {
            SampleSize = parameters.SampleSize,
            RandomSeed = parameters.RandomSeed,
          }
        ),
        input: catalog.MagicAstCardInputs,
        output: catalog.MagicAstAnalysisResults
      );

      // Node 3: Generate diagnostic reports
      pipeline.AddNode(
        label: "GenerateDiagnosticReports",
        description: "Parses all cards and generates aggregated error/warning reports",
        transform: GenerateDiagnosticReportsNode.Create(
          new GenerateDiagnosticReportsNode.Params
          {
            MaxExamplesPerDiagnostic = parameters.MaxExamplesPerDiagnostic,
            TopDiagnosticsPerAbilityType = parameters.TopDiagnosticsPerAbilityType,
          }
        ),
        input: catalog.MagicAstCardInputs,
        output: (catalog.MagicAstErrorReport, catalog.MagicAstWarningReport)
      );
    });
  }
}
