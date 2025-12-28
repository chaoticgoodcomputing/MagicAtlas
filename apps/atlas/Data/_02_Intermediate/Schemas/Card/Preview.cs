using Flowthru.Abstractions;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// Preview information for a Magic card before official release.
/// </summary>
public record Preview : IStructuredSerializable
{
  public DateTime? PreviewedAt { get; init; }
  public string? SourceUri { get; init; }
  public string? Source { get; init; }
}
