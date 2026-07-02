using Flowthru.Data.Catalog;
using MagicAtlas.Data._06_Models.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Embedding-model artifacts (Layer 6). The catalog items are tiny JSON sidecars
/// (<see cref="ModelArtifactRef"/>) pointing to on-disk model directories; the actual model
/// bytes (safetensors, tokenizer, config files) live under <c>_06_Models/&lt;variant&gt;/</c>
/// and are not transited through Flowthru's marshaller. This sidesteps two compounding
/// limits: System.Text.Json's max-value-length (~556 MB after base64) which blocks
/// mpnet-sized models from a single <c>byte[]</c> tarball, and DirectoryStorageAdapter's
/// non-recursive Load (which would hide nested files like <c>1_Pooling/config.json</c>).
/// </summary>
public partial class Catalog
{
  /// <summary>Reference to the default sentence-transformer checkpoint. Populated by
  /// <c>FineTune.DownloadBaseModel</c>. Sidecar filename intentionally hardcoded to the
  /// current default variant slug — when swapping base models wholesale, update both this
  /// path and <c>Flowthru:Flows:FineTune:DefaultVariant</c> together.</summary>
  public IItem<ModelArtifactRef> DefaultEmbeddingModel =>
    CreateItem(() =>
      Item.Of<ModelArtifactRef>("DefaultEmbeddingModel")
        .Json()
        .AtPath($"{_basePath}/_06_Models/default-nomic-embed-text-v1-5.json")
        .Build()
    );

  /// <summary>Reference to the MTG-tuned model. Populated by
  /// <c>FineTune.FineTuneEmbeddingModel</c>.</summary>
  public IItem<ModelArtifactRef> FineTunedEmbeddingModel =>
    CreateItem(() =>
      Item.Of<ModelArtifactRef>("FineTunedEmbeddingModel")
        .Json()
        .AtPath($"{_basePath}/_06_Models/mtg-nomic-v1.json")
        .Build()
    );
}
