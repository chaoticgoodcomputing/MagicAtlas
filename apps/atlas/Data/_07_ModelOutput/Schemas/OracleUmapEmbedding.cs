using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// UMAP-reduced embedding generated from oracle text embeddings.
/// Contains dimensionality-reduced vector representation of card text using manifold learning.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transformation:</strong> OracleTextEmbedding → OracleUmapEmbedding
/// </para>
/// <para>
/// Each 384-dimensional oracle text embedding is reduced to a lower-dimensional
/// representation using UMAP (Uniform Manifold Approximation and Projection).
/// Unlike PCA, UMAP preserves both local and global structure of the data manifold.
/// </para>
/// <para>
/// <strong>Algorithm:</strong> UMAP - A manifold learning technique that:
/// </para>
/// <list type="bullet">
/// <item>Preserves local neighborhood structure (semantic similarity)</item>
/// <item>Maintains global topological structure (clusters and relationships)</item>
/// <item>Produces better visual separation for visualization</item>
/// <item>Captures non-linear relationships in the data</item>
/// </list>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>High-quality 2D/3D visualization of card embeddings</item>
/// <item>Cluster analysis and card similarity exploration</item>
/// <item>Identifying semantic groupings in card text</item>
/// <item>Interactive visualization and exploration tools</item>
/// </list>
/// <para>
/// <strong>Citation:</strong> McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation
/// and Projection for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// </para>
/// </remarks>
public record OracleUmapEmbedding : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  /// <summary>
  /// Unique identifier for this specific text entry.
  /// Matches the ID from the original oracle embedding.
  /// </summary>
  [SerializedLabel("text_entry_id")]
  public required Guid TextEntryId { get; init; }

  /// <summary>
  /// Scryfall card ID.
  /// </summary>
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  /// <summary>
  /// Type of oracle text entry (full text, keyword ability, etc.).
  /// </summary>
  [SerializedLabel("text_type")]
  public required OracleTextType TextType { get; init; }

  /// <summary>
  /// Original oracle text.
  /// </summary>
  [SerializedLabel("text")]
  public required string Text { get; init; }

  /// <summary>
  /// UMAP-reduced embedding components.
  /// </summary>
  /// <remarks>
  /// Typically 2 or 3 dimensions for visualization.
  /// Default: 2 dimensions
  /// </remarks>
  [SerializedLabel("components")]
  public required float[] Components { get; init; }

  /// <summary>
  /// Number of dimensions in the UMAP embedding.
  /// </summary>
  /// <remarks>
  /// Common values:
  /// - 2: For 2D scatter plots
  /// - 3: For 3D visualizations
  /// </remarks>
  [SerializedLabel("component_dimension")]
  public required int ComponentDimension { get; init; }
}
