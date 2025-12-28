using Flowthru.Abstractions;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// Legality status across all Magic: The Gathering formats.
/// </summary>
public record Legalities : IStructuredSerializable
{
  public bool Standard { get; init; }
  public bool Future { get; init; }
  public bool Historic { get; init; }
  public bool Timeless { get; init; }
  public bool Gladiator { get; init; }
  public bool Pioneer { get; init; }
  public bool Explorer { get; init; }
  public bool Modern { get; init; }
  public bool Legacy { get; init; }
  public bool Pauper { get; init; }
  public bool Vintage { get; init; }
  public bool Penny { get; init; }
  public bool Commander { get; init; }
  public bool Oathbreaker { get; init; }
  public bool StandardBrawl { get; init; }
  public bool Brawl { get; init; }
  public bool Alchemy { get; init; }
  public bool PauperCommander { get; init; }
  public bool Duel { get; init; }
  public bool OldSchool { get; init; }
  public bool Premodern { get; init; }
  public bool Predh { get; init; }
}
