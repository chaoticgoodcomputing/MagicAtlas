namespace MagicAST.Keywords;

using MagicAST.AST.Abilities;

/// <summary>
/// Defines a keyword ability and how it expands to a full ability subtree.
/// Keywords are shorthand as defined in Rules 701 (actions) and 702 (abilities).
/// </summary>
public sealed record KeywordDefinition
{
  /// <summary>
  /// The keyword name (e.g., "Flying", "Protection", "Cycling").
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Reference to the comprehensive rules (e.g., "702.9").
  /// </summary>
  public required string RuleReference { get; init; }

  /// <summary>
  /// What kind of ability this keyword represents when expanded.
  /// </summary>
  public required KeywordCategory Category { get; init; }

  /// <summary>
  /// Whether this keyword takes a parameter (e.g., "Protection from [quality]", "Cycling {cost}").
  /// </summary>
  public bool HasParameter { get; init; }

  /// <summary>
  /// The type of parameter if HasParameter is true.
  /// </summary>
  public KeywordParameterType? ParameterType { get; init; }

  /// <summary>
  /// Factory function that creates the expanded ability.
  /// For parameterized keywords, the parameter string is passed in.
  /// For non-parameterized keywords, parameter will be null.
  /// </summary>
  public required Func<string?, Ability> CreateExpansion { get; init; }
}

/// <summary>
/// The category of ability that a keyword expands to.
/// </summary>
public enum KeywordCategory
{
  /// <summary>Static ability (e.g., Flying, Trample, Vigilance)</summary>
  Static,

  /// <summary>Triggered ability (e.g., Landfall, Afterlife, Annihilator)</summary>
  Triggered,

  /// <summary>Activated ability (e.g., Cycling, Equip, Ninjutsu)</summary>
  Activated,

  /// <summary>Spell ability modifier (e.g., Flashback, Overload)</summary>
  SpellModifier,

  /// <summary>Cost modifier (e.g., Affinity, Convoke, Delve)</summary>
  CostModifier,
}

/// <summary>
/// The type of parameter a keyword can have.
/// </summary>
public enum KeywordParameterType
{
  /// <summary>A quality (e.g., Protection from red, Protection from Demons)</summary>
  Quality,

  /// <summary>A mana cost (e.g., Cycling {2}, Kicker {R})</summary>
  ManaCost,

  /// <summary>A number (e.g., Absorb 2, Bushido 1)</summary>
  Number,

  /// <summary>A creature type (e.g., Changeling has implicit "all")</summary>
  CreatureType,

  /// <summary>A card type (e.g., Affinity for artifacts)</summary>
  CardType,

  /// <summary>A named card or permanent (e.g., Partner with [name])</summary>
  Name,
}
