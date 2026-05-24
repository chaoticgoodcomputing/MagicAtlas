using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using MagicAtlas.Data._00_Config.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;

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

  public IItem<ClusteringConfig> ClusteringConfig =>
    CreateItem(() =>
      Item.Of<ClusteringConfig>("ClusteringConfig")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:Clustering")
        .Build()
    );

  public IItem<ReportingConfig> ReportingConfig =>
    CreateItem(() =>
      Item.Of<ReportingConfig>("ReportingConfig")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:Reporting")
        .Build()
    );

  public IItem<TagLabelingConfig> TagLabelingConfig =>
    CreateItem(() =>
      Item.Of<TagLabelingConfig>("TagLabelingConfig")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:TagLabeling")
        .Build()
    );

  /// <summary>
  /// Hand-curated archetype exemplars (counterspell, evasion, ETB, etc.) — the curated-intent
  /// seed for the tag-labeling pipeline's "config" track. Stored as JSON rather than a config
  /// section because the structure is list-of-records-with-nested-list, which IConfiguration
  /// binding can handle but the Python step marshaller surfaces more naturally as a JSON
  /// catalog item.
  /// </summary>
  public IItem<IEnumerable<TagExemplar>> TagExemplars =>
    CreateItem(() =>
      Item.Of<IEnumerable<TagExemplar>>("TagExemplars")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/tag-exemplars.json")
        .Build()
    );

  /// <summary>
  /// Hand-curated allowlist that maps Scryfall otag slugs to canonical archetypes. Each
  /// entry's <c>aliases</c> are the otag slugs that should be merged under its canonical name;
  /// the colon-delimited <c>canonical_slug</c> encodes hierarchy
  /// (<c>removal:creature</c>, <c>tribal:elf</c>). Maintained via the
  /// <c>scryfall_tag_review.py</c>/<c>scryfall_tag_merge.py</c>/<c>scryfall_tag_apply_merge.py</c>
  /// helper trio.
  /// </summary>
  public IItem<IEnumerable<ScryfallTagCanonical>> ScryfallTagCuration =>
    CreateItem(() =>
      Item.Of<IEnumerable<ScryfallTagCanonical>>("ScryfallTagCuration")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/scryfall-tag-curation.json")
        .Build()
    );

  /// <summary>2D UMAP sweep grid — list of n_neighbors × min_dist values, plus K-NN metric knobs.
  /// JSON-backed so the grid can be edited without recompiling.</summary>
  public IItem<UmapSweep2DConfig> UmapSweep2DConfig =>
    CreateItem(() =>
      Item.Of<UmapSweep2DConfig>("UmapSweep2DConfig")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/umap-sweep-2d-config.json")
        .Build()
    );

  /// <summary>5D UMAP sweep grid — n_neighbors × min_dist × supervision_weight. Cartesian
  /// product enumeration; each combo runs HD→5D supervised. Sweep size grows fast.</summary>
  public IItem<UmapSweep5DConfig> UmapSweep5DConfig =>
    CreateItem(() =>
      Item.Of<UmapSweep5DConfig>("UmapSweep5DConfig")
        .Json()
        .AtPath($"{_basePath}/_00_Config/Datasets/umap-sweep-5d-config.json")
        .Build()
    );
}
