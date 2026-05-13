using Flowthru.Data.Schema;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// Price information for a card in various currencies and formats.
/// </summary>
[FlowthruSchema]
public partial record Prices
{
  public decimal? Usd { get; init; }
  public decimal? UsdFoil { get; init; }
  public decimal? UsdEtched { get; init; }
  public decimal? Eur { get; init; }
  public decimal? EurFoil { get; init; }
  public decimal? EurEtched { get; init; }
  public decimal? Tix { get; init; }
}
