using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using MagicAtlas.Data._00_Config.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Configuration catalog entries (Layer 0). Each item binds lazily, on Load, against an
/// <c>IConfiguration</c> section under <c>Flowthru:Flows:&lt;name&gt;</c> in
/// <c>appsettings.json</c> (see harness <c>Program.cs</c>). No on-disk sidecar — Flowthru's
/// <c>FromConfiguration</c> adapter pulls the value straight from the live configuration tree
/// and rolls it into the cache plan, so editing a section automatically invalidates the
/// downstream cache without a separate materialization step.
/// </summary>
public partial class Catalog
{
  public IItem<FineTuneConfig> FineTuneConfig =>
    CreateItem(() =>
      Item.Of<FineTuneConfig>("FineTuneConfig")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:FineTune")
        .Build()
    );

  public IItem<OracleEmbeddingConfig> OracleEmbeddingConfig =>
    CreateItem(() =>
      Item.Of<OracleEmbeddingConfig>("OracleEmbeddingConfig")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:OracleEmbedding")
        .Build()
    );

  public IItem<ReportingConfig> ReportingConfig =>
    CreateItem(() =>
      Item.Of<ReportingConfig>("ReportingConfig")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:Reporting")
        .Build()
    );
}
