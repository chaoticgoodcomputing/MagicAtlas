using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// UMAP-reduced embedding with associated hyperparameters from a grid search exploration.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transformation:</strong> OracleTextEmbedding → UmapHyperparameterScanResult
/// </para>
/// <para>
/// Each result represents a single oracle text entry's UMAP embedding generated with
/// specific hyperparameter values. Multiple results with the same TextEntryId but
/// different hyperparameters enable comparison of how UMAP settings affect the
/// embedding space structure.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Hyperparameter sensitivity analysis</item>
/// <item>Grid search for optimal UMAP visualization parameters</item>
/// <item>Side-by-side comparison of different UMAP configurations</item>
/// <item>Understanding how NumberOfNeighbors and MinDist affect clustering</item>
/// </list>
/// <para>
/// <strong>Visualization Strategy:</strong> Results are typically displayed in a grid
/// layout where each subplot represents one hyperparameter combination, with normalized
/// coordinates for cross-comparison.
/// </para>
/// </remarks>
public record UmapHyperparameterScanResult
  : IFlatSchema,
    IBinarySerializable,
    IStructuredSerializable
{
  /// <summary>
  /// Unique identifier for this specific text entry.
  /// </summary>
  [SerializedLabel("text_entry_id")]
  public required Guid TextEntryId { get; init; }

  /// <summary>
  /// Scryfall card ID.
  /// </summary>
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  /// <summary>
  /// Type of oracle text entry (keyword ability, triggered ability, etc.).
  /// </summary>
  [SerializedLabel("text_type")]
  public required OracleTextType TextType { get; init; }

  /// <summary>
  /// Original oracle text.
  /// </summary>
  [SerializedLabel("text")]
  public required string Text { get; init; }

  /// <summary>
  /// UMAP embedding components (typically 2D for visualization).
  /// </summary>
  [SerializedLabel("components")]
  public required float[] Components { get; init; }

  /// <summary>
  /// Number of dimensions in the UMAP embedding.
  /// </summary>
  [SerializedLabel("component_dimension")]
  public required int ComponentDimension { get; init; }

  /// <summary>
  /// UMAP hyperparameter: Number of neighboring points used in manifold approximation.
  /// </summary>
  /// <remarks>
  /// Controls the balance between local and global structure preservation.
  /// Larger values emphasize global structure, smaller values emphasize local neighborhoods.
  /// </remarks>
  [SerializedLabel("number_of_neighbors")]
  public required int NumberOfNeighbors { get; init; }

  /// <summary>
  /// UMAP hyperparameter: Minimum distance between points in the low-dimensional embedding.
  /// </summary>
  /// <remarks>
  /// Controls how tightly points are packed. Smaller values allow tighter clustering,
  /// larger values produce more evenly distributed embeddings.
  /// </remarks>
  [SerializedLabel("min_dist")]
  public required float MinDist { get; init; }

  /// <summary>
  /// Distance metric used for high-dimensional similarity calculation.
  /// </summary>
  [SerializedLabel("metric")]
  public required string Metric { get; init; }

  /// <summary>
  /// Random seed used for UMAP initialization.
  /// </summary>
  [SerializedLabel("random_seed")]
  public required int RandomSeed { get; init; }
}
