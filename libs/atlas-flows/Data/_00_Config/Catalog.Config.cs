using Flowthru.Data.Catalog;
using MagicAtlas.Data._00_Config.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Configuration catalog entries (Layer 0). Each item is a JSON sidecar materialized at harness
/// startup from <c>appsettings.json</c>'s <c>Flowthru:Flows:*</c> sections, then consumed by
/// steps the same way ordinary data items are. The two-step path (appsettings.json →
/// IConfiguration → sidecar → catalog item → step) lets C# and Python steps share a single
/// source of truth without dragging <c>IConfiguration</c> across the Python boundary.
/// </summary>
public partial class Catalog
{
  public IItem<FineTuneConfig> FineTuneConfig =>
    CreateItem(() =>
      Item.Of<FineTuneConfig>("FineTuneConfig")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/finetune.json")
        .Build()
    );

  public IItem<OracleEmbeddingConfig> OracleEmbeddingConfig =>
    CreateItem(() =>
      Item.Of<OracleEmbeddingConfig>("OracleEmbeddingConfig")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/oracle-embedding.json")
        .Build()
    );

  public IItem<ClusteringConfig> ClusteringConfig =>
    CreateItem(() =>
      Item.Of<ClusteringConfig>("ClusteringConfig")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/clustering.json")
        .Build()
    );

  public IItem<ReportingConfig> ReportingConfig =>
    CreateItem(() =>
      Item.Of<ReportingConfig>("ReportingConfig")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/reporting.json")
        .Build()
    );

  public IItem<ModelEvaluationsConfig> ModelEvaluationsConfig =>
    CreateItem(() =>
      Item.Of<ModelEvaluationsConfig>("ModelEvaluationsConfig")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/model-evaluations.json")
        .Build()
    );
}
