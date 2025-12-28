using Flowthru.Data;
using MagicAtlas.Data._04_Feature.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Feature data catalog entries (Layer 4).
/// Contains analytics-specific features and transformations for embedding analysis.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Refined oracle text with expanded symbols and categorized abilities.
  /// Persisted to disk as JSON.
  /// </summary>
  public ICatalogEntry<IEnumerable<RefinedOracleText>> RefinedOracleText =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<RefinedOracleText>(
          label: "RefinedOracleText",
          filePath: $"{_basePath}/_04_Feature/Datasets/refined-oracle-text.json"
        )
    );

  /// <summary>
  /// Tokenized oracle text entries ready for ONNX model inference.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Transformation:</strong> EmbeddingModelOracleInput → TokenizedBertInput
  /// </para>
  /// <para>
  /// Contains BERT token tensors (input_ids, attention_mask, token_type_ids) for each
  /// oracle text entry that passed tokenization validation.
  /// </para>
  /// <para>
  /// <strong>Storage:</strong> In-memory only (not persisted)
  /// </para>
  /// <para>
  /// <strong>Lifecycle:</strong> Created during tokenization, consumed by ONNX inference,
  /// then garbage collected.
  /// </para>
  /// <para>
  /// <strong>Layer:</strong> 4 (Feature - transient data)
  /// </para>
  /// </remarks>
  public ICatalogEntry<IEnumerable<TokenizedBertInput>> TokenizedOracleText =>
    GetOrCreateEntry(
      () => CatalogEntries.Enumerable.Memory<TokenizedBertInput>(label: "TokenizedOracleText")
    );
}
