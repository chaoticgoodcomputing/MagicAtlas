using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Selects which HuggingFace sentence-transformers repos serve as the default-variant model and
/// the fine-tune starting point. Pulled in by the FineTune flow's <c>DownloadBaseModel</c> and
/// <c>FineTuneEmbeddingModel</c> steps so that switching base models is a JSON edit, not a code
/// change. Stored as a single record (not a list) — there's only one base-model spec per
/// pipeline configuration.
/// </summary>
[FlowthruSchema]
public partial record BaseModelSpec
{
  /// <summary>
  /// HuggingFace repo id served as <c>DefaultEmbeddingModel</c> — the production embedder used
  /// by <c>EmbedOracleText</c> until the fine-tuned variant is wired in. Default:
  /// <c>"sentence-transformers/all-MiniLM-L6-v2"</c>.
  /// </summary>
  [SerializedLabel("default_repo_id")]
  public required string DefaultRepoId { get; init; }

  /// <summary>
  /// HuggingFace repo id used as the starting point for fine-tuning. Default:
  /// <c>"sentence-transformers/all-mpnet-base-v2"</c> — 768-dim, 110M params, more headroom
  /// than the default-variant MiniLM to encode the MTG-mechanical distinctions the fine-tune
  /// is teaching.
  /// </summary>
  [SerializedLabel("finetune_base_repo_id")]
  public required string FineTuneBaseRepoId { get; init; }
}
