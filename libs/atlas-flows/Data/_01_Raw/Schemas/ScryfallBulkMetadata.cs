using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Scryfall bulk-data metadata envelope returned by
/// <c>https://api.scryfall.com/bulk-data/{id}</c>. The actual bulk JSON lives at
/// <see cref="DownloadUri"/>, a daily-rotating CDN URL on <c>data.scryfall.io</c>; we hit the
/// stable metadata endpoint once per run to discover the current bulk URL and then let Flowthru's
/// HTTP-cached storage medium fetch the body itself.
/// </summary>
[FlowthruSchema]
public partial record ScryfallBulkMetadata
{
  [SerializedLabel("object")]
  public string Object { get; init; } = "";

  [SerializedLabel("id")]
  public string Id { get; init; } = "";

  [SerializedLabel("type")]
  public string Type { get; init; } = "";

  [SerializedLabel("name")]
  public string Name { get; init; } = "";

  [SerializedLabel("description")]
  public string Description { get; init; } = "";

  [SerializedLabel("download_uri")]
  public string DownloadUri { get; init; } = "";

  [SerializedLabel("size")]
  public long Size { get; init; }

  [SerializedLabel("updated_at")]
  public string UpdatedAt { get; init; } = "";

  [SerializedLabel("content_type")]
  public string ContentType { get; init; } = "";
}
