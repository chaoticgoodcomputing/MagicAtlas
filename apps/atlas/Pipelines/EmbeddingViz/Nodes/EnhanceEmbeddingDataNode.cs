using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data.Enums.Card;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Pipelines.EmbeddingViz.Nodes;

/// <summary>
/// Enhances UMAP embeddings with card metadata for rich visualizations.
/// </summary>
/// <remarks>
/// <para>
/// Performs an inner join between UMAP embeddings and card core data, adding:
/// </para>
/// <list type="bullet">
/// <item>Card name for hover tooltips</item>
/// <item>Color identity for color-coded visualizations</item>
/// <item>Mana value (CMC) for size-based visualizations</item>
/// </list>
/// <para>
/// <strong>Join Strategy:</strong> Inner join on CardId
/// </para>
/// <para>
/// Embeddings without matching card data will be excluded from the output.
/// </para>
/// </remarks>
public static class EnhanceEmbeddingDataNode
{
  /// <summary>
  /// Creates an enhancement function that joins UMAP embeddings with card metadata.
  /// </summary>
  /// <param name="logger">Optional logger for diagnostic output.</param>
  /// <returns>
  /// A function that takes UMAP embeddings and card data, returning enhanced embeddings.
  /// </returns>
  public static Func<
    (IEnumerable<OracleUmapEmbedding>, IEnumerable<CardCoreData>),
    Task<(IEnumerable<EnhancedUmapEmbedding>, IEnumerable<EnhancedUmapFlattenedEmbedding>)>
  > Create(ILogger? logger = null)
  {
    return async (input) =>
    {
      var (embeddings, cardData) = input;

      var embeddingsList = embeddings.ToList();
      var cardDataDict = cardData.ToDictionary(c => c.Id);

      logger?.LogInformation(
        "Enhancing {EmbeddingCount} UMAP embeddings with card metadata from {CardCount} cards",
        embeddingsList.Count,
        cardDataDict.Count
      );

      var enhanced = embeddingsList
        .Where(e => cardDataDict.ContainsKey(e.CardId))
        .Select(e =>
        {
          var card = cardDataDict[e.CardId];
          return new EnhancedUmapEmbedding
          {
            TextEntryId = e.TextEntryId,
            CardId = e.CardId,
            TextType = e.TextType,
            Text = e.Text,
            Components = e.Components,
            ComponentDimension = e.ComponentDimension,
            CardName = card.Name,
            ColorIdentity = card.ColorIdentity,
            Cmc = card.Cmc,
            Price = card.Prices?.Usd ?? card.Prices?.UsdFoil ?? card.Prices?.UsdEtched ?? 0.0m,
          };
        })
        .ToList();

      var excludedCount = embeddingsList.Count - enhanced.Count;
      if (excludedCount > 0)
      {
        logger?.LogWarning(
          "Excluded {ExcludedCount} embeddings without matching card data",
          excludedCount
        );
      }

      logger?.LogInformation(
        "Successfully enhanced {EnhancedCount} embeddings with card metadata",
        enhanced.Count
      );

      var enhancedFlattened = enhanced.Select(e => new EnhancedUmapFlattenedEmbedding
      {
        TextEntryId = e.TextEntryId,
        CardId = e.CardId,
        TextType = e.TextType,
        Text = e.Text,
        Component0 = e.ComponentDimension > 0 ? e.Components[0] : 0.0f,
        Component1 = e.ComponentDimension > 1 ? e.Components[1] : 0.0f,
        ComponentDimension = e.ComponentDimension,
        CardName = e.CardName,
        IsWhite = e.ColorIdentity != null && e.ColorIdentity.Contains(ManaColor.White),
        IsBlue = e.ColorIdentity != null && e.ColorIdentity.Contains(ManaColor.Blue),
        IsBlack = e.ColorIdentity != null && e.ColorIdentity.Contains(ManaColor.Black),
        IsRed = e.ColorIdentity != null && e.ColorIdentity.Contains(ManaColor.Red),
        IsGreen = e.ColorIdentity != null && e.ColorIdentity.Contains(ManaColor.Green),
        Cmc = e.Cmc,
        Price = e.Price,
      });

      return await Task.FromResult((enhanced.AsEnumerable(), enhancedFlattened.AsEnumerable()));
    };
  }
}
