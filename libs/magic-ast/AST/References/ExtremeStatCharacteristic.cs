namespace MagicAST.AST.References;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A superlative constraint — the filtered object must hold the greatest (or least)
/// value of a numeric characteristic among a population of objects. "a creature or
/// planeswalker they control with the greatest mana value among creatures and
/// planeswalkers they control" (Blot Out, End of the Hunt); the comparative predicate
/// carved out of the <see cref="OtherCharacteristic"/> residual that the free-text
/// whitelist named as missing ("no MaxStatFilter node exists yet", the Professor Onyx
/// greatest-power carve-out).
///
/// <para>
/// A game-state predicate, not machinery: it records what the oracle text says (this
/// object is the maximum/minimum of <see cref="Stat"/> over the population), never how
/// the extremum is computed. CR 202.3 defines mana value; CR 208 power/toughness.
/// Ties are resolved by the choosing player at resolution (CR 701 edict choice) — the
/// predicate merely names which end of the range qualifies.
/// </para>
///
/// <para>
/// The population the extremum ranges over is, by default, the enclosing
/// <see cref="ObjectFilter"/>'s own set — the "among …" clause restates the object's
/// own filter ("greatest mana value among creatures and planeswalkers they control" on
/// a filter that already is "creature or planeswalker they control"). When the ranged
/// population differs from the object's own filter, <see cref="Scope"/> names it
/// explicitly; null (the common case) means "among the objects matching this same
/// filter".
/// </para>
/// </summary>
[CharacteristicKind("extremeStat")]
public sealed record ExtremeStatCharacteristic : Characteristic
{
  /// <summary>Which numeric characteristic is compared — power, toughness, or mana value.</summary>
  public required RelativeCharacteristic Stat { get; init; }

  /// <summary>Whether the object holds the greatest or the least value of <see cref="Stat"/>.</summary>
  public required StatExtreme Extreme { get; init; }

  /// <summary>
  /// The population the extremum ranges over, when it is NOT the enclosing filter's own
  /// set. Null (the common case) means "among the objects matching this same filter".
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Scope { get; init; }
}

/// <summary>Which end of the numeric range an <see cref="ExtremeStatCharacteristic"/> selects.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatExtreme
{
  /// <summary>"the greatest [stat]" — the maximum.</summary>
  Greatest,

  /// <summary>"the least [stat]" / "the lowest [stat]" — the minimum.</summary>
  Least,
}
