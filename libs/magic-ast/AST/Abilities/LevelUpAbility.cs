namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents the Level Up superstructure on Leveler creatures (Rule 711,
/// Rise of the Eldrazi). The level-up cost is an activated ability that
/// adds a level counter to the creature; the LEVEL N-M stanzas describe
/// the creature's characteristics (P/T and inner abilities) at each level
/// range.
/// </summary>
[OracleAbility("levelUp")]
public sealed record LevelUpAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.LevelUp;

  /// <summary>
  /// The "Level up {cost}" activated ability that puts level counters on
  /// this creature. The cost is whatever the parser extracted from the
  /// oracle text (mana, sometimes alternative costs).
  /// </summary>
  public required ActivatedAbility LevelUpCost { get; init; }

  /// <summary>The level stanzas, in oracle-text order.</summary>
  public required IReadOnlyList<LevelStanza> Stanzas { get; init; }
}

/// <summary>
/// One "LEVEL N-M" or "LEVEL N+" stanza on a Leveler. While the creature
/// has level counters within this range, the listed P/T and abilities
/// apply.
/// </summary>
public sealed record LevelStanza
{
  /// <summary>Lower bound of the level range (inclusive).</summary>
  public required int MinLevel { get; init; }

  /// <summary>Upper bound (inclusive). Null for open-ended "N+" stanzas.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MaxLevel { get; init; }

  /// <summary>The creature's power while at this level range.</summary>
  public required PowerToughnessValue Power { get; init; }

  /// <summary>The creature's toughness while at this level range.</summary>
  public required PowerToughnessValue Toughness { get; init; }

  /// <summary>
  /// Additional abilities the creature has while at this level range.
  /// Most stanzas have zero or one. Dispatched through the registry like
  /// any other body abilities.
  /// </summary>
  public IReadOnlyList<Ability> Abilities { get; init; } = [];
}
