using Flowthru.Data.Schema;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// URIs to card images in various sizes and formats.
/// </summary>
[FlowthruSchema]
public partial record ImageUris
{
  public string? Small { get; init; }
  public string? Normal { get; init; }
  public string? Large { get; init; }
  public string? Png { get; init; }
  public string? ArtCrop { get; init; }
  public string? BorderCrop { get; init; }
}
