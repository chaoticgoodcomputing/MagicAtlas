using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// Configuration for the ModelEvaluations flow — currently just the display labels stamped on
/// each <c>ModelEvaluationResult</c> row so the variant identity travels with the eval output.
/// Materialized at startup from <c>Flowthru:Flows:ModelEvaluations</c> in <c>appsettings.json</c>.
/// </summary>
[FlowthruSchema]
public partial record ModelEvaluationsConfig
{
  /// <summary>Variant label written into <c>ModelEvaluationResult.ModelVariant</c> for the default chain.</summary>
  public string DefaultVariantLabel { get; init; } = "";

  /// <summary>Variant label written into <c>ModelEvaluationResult.ModelVariant</c> for the fine-tuned chain.</summary>
  public string FineTunedVariantLabel { get; init; } = "";
}
