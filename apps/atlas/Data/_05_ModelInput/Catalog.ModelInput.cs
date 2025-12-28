using Flowthru.Abstractions;
using Flowthru.Data;
using MagicAST;
using MagicAtlas.Data._05_ModelInput.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Model input data catalog entries (Layer 5).
/// Contains master tables with all features ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Flattened oracle text entries for embedding model input.
  /// Each card produces multiple entries (full text + individual abilities).
  /// </summary>
  public ICatalogEntry<IEnumerable<EmbeddingModelOracleInput>> EmbeddingModelOracleInput =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<EmbeddingModelOracleInput>(
          label: "EmbeddingModelOracleInput",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/embedding-model-oracle-input.csv"
        )
    );

  /// <summary>
  /// CardInputDTO for MagicAST parsing - mapped from CardCoreData.
  /// In-memory only, not persisted to disk.
  /// </summary>
  public ICatalogEntry<IEnumerable<CardInputDTO>> MagicAstCardInputs =>
    GetOrCreateEntry(
      () => CatalogEntries.Enumerable.Memory<CardInputDTO>(label: "MagicAstCardInputs")
    );
}
