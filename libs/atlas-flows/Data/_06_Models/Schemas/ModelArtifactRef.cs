using Flowthru.Data.Schema;

namespace MagicAtlas.Data._06_Models.Schemas;

/// <summary>
/// Reference to a sentence-transformers model directory on disk. Used in place of a
/// <c>byte[]</c> (tarball) catalog item because Flowthru's subprocess marshaller pipes step
/// payloads through a JSON envelope, and a 400+ MB mpnet checkpoint exceeds
/// System.Text.Json's max-value-length on the C# decode side. Storing a tiny JSON metadata
/// file that points to the on-disk model dir sidesteps the marshaller for the model bytes.
/// </summary>
/// <remarks>
/// <para>
/// The producing Python step (DownloadBaseModel / FineTuneEmbeddingModel) writes the model
/// files directly to <see cref="Path"/> and emits this ref as the step's catalog output. The
/// consuming step (EmbedOracleText / EmbedOracleTextFineTuned) reads the ref, then loads the
/// model from <see cref="Path"/> with <c>SentenceTransformer(path)</c>.
/// </para>
/// <para>
/// <see cref="Path"/> is an absolute filesystem path. The harness exposes its data-dir root
/// to Python steps via the <c>MAGIC_ATLAS_DATA</c> env var so steps can construct paths under
/// <c>_06_Models/</c> without learning the catalog's <c>_basePath</c> indirectly.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record ModelArtifactRef
{
  /// <summary>Absolute path to the sentence-transformers model directory.</summary>
  [SerializedLabel("path")]
  public required string Path { get; init; }

  /// <summary>HuggingFace repo id the model was sourced from (for default) or fine-tuned
  /// from (for the fine-tuned variant). Provenance metadata.</summary>
  [SerializedLabel("repo_id")]
  public required string RepoId { get; init; }

  /// <summary>Stable identifier for this variant — e.g. <c>"default-minilm-l6-v2"</c>,
  /// <c>"mtg-mpnet-v1"</c>. Surfaces into <c>ModelEvaluationResult.ModelVariant</c>.</summary>
  [SerializedLabel("variant")]
  public required string Variant { get; init; }
}
