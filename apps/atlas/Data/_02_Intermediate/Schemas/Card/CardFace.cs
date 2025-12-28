using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents one face of a multi-faced Magic card.
/// </summary>
public record CardFace : IStructuredSerializable
{
  public string? Artist { get; init; }
  public Guid? ArtistId { get; init; }
  public decimal? Cmc { get; init; }
  public List<ManaColor>? ColorIndicator { get; init; }
  public List<ManaColor>? Colors { get; init; }
  public string? Defense { get; init; }
  public string? FlavorText { get; init; }
  public Guid? IllustrationId { get; init; }
  public ImageUris? ImageUris { get; init; }
  public Layout? Layout { get; init; }
  public string? Loyalty { get; init; }
  public string ManaCost { get; init; } = "";
  public string Name { get; init; } = "";
  public Guid? OracleId { get; init; }
  public string? OracleText { get; init; }
  public string? Power { get; init; }
  public string? PrintedName { get; init; }
  public string? PrintedText { get; init; }
  public string? PrintedTypeLine { get; init; }
  public string? Toughness { get; init; }
  public HashSet<string>? Types { get; init; }
  public HashSet<string>? Subtypes { get; init; }
  public string? Watermark { get; init; }
}
