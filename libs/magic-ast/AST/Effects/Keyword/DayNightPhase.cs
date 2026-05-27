namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;

/// <summary>
/// The day/night phase for the Daybound and Nightbound keyword abilities.
/// Rule 702.145.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DayNightPhase
{
  /// <summary>
  /// Daybound — found on the front faces of day/night double-faced cards. Rule 702.145b.
  /// </summary>
  Daybound,

  /// <summary>
  /// Nightbound — found on the back faces of day/night double-faced cards. Rule 702.145e.
  /// </summary>
  Nightbound,
}
