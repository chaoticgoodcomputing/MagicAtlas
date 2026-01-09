using Flowthru.Data;
using Flowthru.Integrations.MLNet;

namespace MagicAtlas.Data;

/// <summary>
/// Model data catalog entries (Layer 6).
/// Contains pre-trained machine learning models and their configurations.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// ONNX model file for all-MiniLM-L6-v2 sentence-transformers model (Layer 0 seed data).
  ///
  /// Retrieved from https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/tree/main/onnx
  /// </summary>
  public ICatalogEntry<byte[]> MiniLmOnnxModel =>
    GetOrCreateEntry(
      () =>
        CatalogEntriesMLNet.OnnxModel(
          label: "MiniLmOnnxModel",
          filePath: $"{_basePath}/_06_Models/Datasets/Pretrained/all-MiniLM-L6-v2/model.onnx"
        )
    );

  /// <summary>
  /// Vocabulary file for all-MiniLM-L6-v2 model (Layer 0 seed data).
  ///
  /// Retrieved from https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/tree/main
  /// </summary>
  public ICatalogEntry<string> MiniLmVocabFile =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "MiniLmVocabFile",
          filePath: $"{_basePath}/_06_Models/Datasets/Pretrained/all-MiniLM-L6-v2/vocab.txt"
        )
    );
}
