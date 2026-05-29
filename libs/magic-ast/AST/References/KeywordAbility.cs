namespace MagicAST.AST.References;

using System.Text.Json.Serialization;

/// <summary>
/// Canonical identity of a Magic keyword ability (CR 702). A structured
/// alternative to the bare keyword strings the AST has historically carried —
/// casing-proof and exhaustively matchable.
///
/// <para>
/// Seeded with the keyword abilities currently structured by
/// <see cref="KeywordCharacteristic"/> (the evasion-relevant keywords that
/// appear inside <see cref="ObjectFilter.Characteristics"/>). It grows as
/// further keyword-as-string sites are subsumed — notably the planned
/// migration of <c>Ability.KeywordSource</c>. Only parameterless keyword
/// abilities belong here; parameterized keywords (Protection from …, Enchant …,
/// landcycling) carry their parameter separately and are added when that
/// migration lands.
/// </para>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KeywordAbility
{
  /// <summary>Flying (CR 702.9).</summary>
  Flying,

  /// <summary>Reach (CR 702.17).</summary>
  Reach,

  /// <summary>Shadow (CR 702.28).</summary>
  Shadow,
}
