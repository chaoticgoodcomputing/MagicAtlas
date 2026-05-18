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

  public int TrainNumEpochs { get; init; }
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
