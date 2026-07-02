using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// Configuration for the FineTune flow — both base-model selection and training hyperparameters.
/// Materialized from <c>Flowthru:Flows:FineTune</c> in <c>appsettings.json</c> at harness startup
/// (see <c>Program.cs</c>) into the JSON sidecar that backs the <c>FineTuneConfig</c> catalog
/// item, so Python steps can read it like any other scalar input.
/// </summary>
/// <remarks>
/// All fields are flat scalars: Flowthru's Python step marshaller only encodes primitive types
/// (numbers/bool/string/byte[]/enums/arrays of same), so nested POCOs aren't allowed in step
/// inputs. Field names use the <c>Train*</c> prefix to keep training-loop knobs visibly grouped
/// without nesting them in a sub-record. Scalar <c>IItem&lt;T&gt;</c> records are deserialized
/// in Python by C# PascalCase property names — the <c>[SerializedLabel]</c> snake_case
/// attribute applies to tabular columns only.
/// </remarks>
[FlowthruSchema]
public partial record FineTuneConfig
{
  /// <summary>HuggingFace repo id for the default (un-fine-tuned) embedder.</summary>
  public string DefaultRepoId { get; init; } = "";

  /// <summary>On-disk directory name under <c>_06_Models/</c> for the default variant.</summary>
  public string DefaultVariant { get; init; } = "";

  /// <summary>HuggingFace repo id used as the fine-tune starting point.</summary>
  public string FineTuneBaseRepoId { get; init; } = "";

  /// <summary>On-disk directory name under <c>_06_Models/</c> for the fine-tuned variant.</summary>
  public string FineTuneVariant { get; init; } = "";

  // ── SentenceTransformer training-loop knobs ──

  /// <summary>
  /// Number of fine-tune epochs. Nomic Embed's own technical report is explicit:
  /// "training for multiple epochs hurts performance" — they used a single epoch on a much
  /// larger corpus than ours.
  /// <para>
  /// Source: <i>Nomic Embed: Training a Reproducible Long Context Text Embedder</i>,
  /// Nussbaum et al., arxiv:2402.01613 §3.1.
  /// </para>
  /// </summary>
  public int TrainNumEpochs { get; init; }

  /// <summary>
  /// Effective batch size for the contrastive loss. With MultipleNegativesRankingLoss family
  /// losses, each anchor's "negatives" are the OTHER items in the same batch — so batch size
  /// is the dominant lever on negative-sample quality, NOT just a memory/throughput knob.
  /// <para>
  /// sbert.net's training overview recommends a floor of 16–64; Nomic's reference training
  /// used a 16,384 global batch. The Cached variant of MNR (see
  /// <c>fine_tune_embedding_model.py</c>) lets us run at the recommended effective batch size
  /// without exceeding GPU memory by chunking the loss internally.
  /// </para>
  /// <para>
  /// Sources: <see href="https://sbert.net/docs/sentence_transformer/training_overview.html"/>
  /// and Nomic Embed paper arxiv:2402.01613 §3.1.
  /// </para>
  /// </summary>
  public int TrainPerDeviceBatchSize { get; init; }

  public double TrainWarmupRatio { get; init; }
  public double TrainLearningRate { get; init; }
  public int TrainLoggingSteps { get; init; }
  public bool TrainFp16 { get; init; }

  /// <summary>
  /// Multiplier applied to <c>(anchor, positive)</c> pairs originating from a curated triplet.
  /// Triplets carry an explicit hard negative so they're stronger signal than auto-extracted
  /// pairs; the loss-side weighting bumps their effective contribution to MNR.
  /// </summary>
  public double TripletWeight { get; init; }
}
