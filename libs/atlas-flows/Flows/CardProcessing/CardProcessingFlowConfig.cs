using MagicAtlas.Flows.CardProcessing.Nodes;
using Microsoft.Extensions.Configuration;

namespace MagicAtlas.Flows.CardProcessing;

/// <summary>
/// Configuration adapter for <see cref="CardProcessingFlow"/>. Binds the
/// <c>Flowthru:Flows:CardProcessing</c> section of <c>appsettings.json</c> into
/// <see cref="FilterAndSplitCardsNode.FilterOptions"/>, which the flow consumes when
/// filtering and splitting cards into core data and metadata.
/// </summary>
public sealed class CardProcessingFlowConfig
{
  public FilterAndSplitCardsNode.FilterOptions FilterOptions { get; }

  public CardProcessingFlowConfig(IConfiguration configuration)
  {
    if (configuration is null)
      throw new ArgumentNullException(nameof(configuration));
    FilterOptions =
      configuration
        .GetSection("Flowthru:Flows:CardProcessing:FilterOptions")
        .Get<FilterAndSplitCardsNode.FilterOptions>() ?? new FilterAndSplitCardsNode.FilterOptions();
  }
}
