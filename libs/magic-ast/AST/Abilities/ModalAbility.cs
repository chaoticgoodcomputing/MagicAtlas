namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents a modal ability where the player chooses between options.
/// e.g., "Choose one —", "Choose two —", "Choose one or both —"
/// </summary>
[OracleAbility("modal")]
public sealed record ModalAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Spell; // Modal modifies another ability type

  /// <summary>
  /// How many modes must/can be chosen.
  /// </summary>
  public required ModeSelection ModeSelection { get; init; }

  /// <summary>
  /// The available modes to choose from.
  /// </summary>
  public required IReadOnlyList<ModalOption> Modes { get; init; }

  /// <summary>
  /// Whether the same mode can be chosen more than once.
  /// e.g., "Choose three. You may choose the same mode more than once."
  /// </summary>
  public bool AllowDuplicates { get; init; }
}

/// <summary>
/// Describes how modes are selected for a modal ability.
/// </summary>
public sealed record ModeSelection
{
  /// <summary>
  /// The minimum number of modes that must be chosen.
  /// </summary>
  public required int Minimum { get; init; }

  /// <summary>
  /// The maximum number of modes that can be chosen.
  /// </summary>
  public required int Maximum { get; init; }

  /// <summary>
  /// Optional condition that changes mode selection.
  /// e.g., "choose one unless you control a creature, then choose both"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ModeSelectionOverride? ConditionalOverride { get; init; }

  /// <summary>
  /// Creates a "Choose one" selection.
  /// </summary>
  public static ModeSelection ChooseOne() => new() { Minimum = 1, Maximum = 1 };

  /// <summary>
  /// Creates a "Choose two" selection.
  /// </summary>
  public static ModeSelection ChooseTwo() => new() { Minimum = 2, Maximum = 2 };

  /// <summary>
  /// Creates a "Choose one or both" selection.
  /// </summary>
  public static ModeSelection ChooseOneOrBoth() => new() { Minimum = 1, Maximum = 2 };

  /// <summary>
  /// Creates a "Choose N" selection.
  /// </summary>
  public static ModeSelection ChooseExactly(int n) => new() { Minimum = n, Maximum = n };

  /// <summary>
  /// Creates an "up to N" selection.
  /// </summary>
  public static ModeSelection ChooseUpTo(int n) => new() { Minimum = 0, Maximum = n };

  /// <summary>
  /// Creates a "Choose one or more" selection over <paramref name="modeCount"/> available
  /// modes: at least one, at most all of them.
  /// </summary>
  public static ModeSelection ChooseOneOrMore(int modeCount) =>
    new() { Minimum = 1, Maximum = modeCount };
}

/// <summary>
/// A conditional override for mode selection.
/// </summary>
public sealed record ModeSelectionOverride
{
  /// <summary>
  /// The condition that triggers the override.
  /// </summary>
  public required Condition Condition { get; init; }

  /// <summary>
  /// The mode selection to use when the condition is met.
  /// </summary>
  public required ModeSelection Selection { get; init; }
}

/// <summary>
/// A single option in a modal ability.
/// </summary>
public sealed record ModalOption
{
  /// <summary>
  /// The ability that occurs if this mode is chosen.
  /// </summary>
  public required Ability Ability { get; init; }

  /// <summary>
  /// Optional name for the mode (used in some cards like Dawnbringer Cleric).
  /// e.g., "Cure Wounds", "Dispel Magic"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Name { get; init; }
}
