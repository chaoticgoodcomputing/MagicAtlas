namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Daybound/Nightbound (Rule 702.145). Keyword abilities found on the front and
/// back faces, respectively, of day/night double-faced cards. Daybound is on
/// the front face; nightbound is on the back face. MAST records the keyword's
/// presence and which phase it belongs to; the day/night transformation rules
/// (Rule 731), state-based checks, and transformation triggers are engine
/// territory per the descriptive-not-engine doctrine.
/// </summary>
[OracleEffect("dayNight")]
public sealed record DayNightEffect : Effect
{
  /// <summary>
  /// Which side of the day/night mechanic this keyword represents.
  /// "Daybound" = front face; "Nightbound" = back face. Rule 702.145b/702.145e.
  /// </summary>
  public required DayNightPhase Phase { get; init; }
}
