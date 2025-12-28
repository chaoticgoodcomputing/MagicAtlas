namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a modal ability where the player chooses between options.
/// e.g., "Choose one —", "Choose two —", "Choose one or both —"
/// </summary>
public sealed record ModalAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Spell; // Modal modifies another ability type

  /// <summary>
  /// How many modes must/can be chosen.
  /// </summary>
  [JsonPropertyName("modeSelection")]
  public required ModeSelection ModeSelection { get; init; }

  /// <summary>
  /// The available modes to choose from.
  /// </summary>
  [JsonPropertyName("modes")]
  public required IReadOnlyList<ModalOption> Modes { get; init; }

  /// <summary>
  /// Whether the same mode can be chosen more than once.
  /// e.g., "Choose three. You may choose the same mode more than once."
  /// </summary>
  [JsonPropertyName("allowDuplicates")]
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
  [JsonPropertyName("minimum")]
  public required int Minimum { get; init; }

  /// <summary>
  /// The maximum number of modes that can be chosen.
  /// </summary>
  [JsonPropertyName("maximum")]
  public required int Maximum { get; init; }

  /// <summary>
  /// Optional condition that changes mode selection.
  /// e.g., "choose one unless you control a creature, then choose both"
  /// </summary>
  [JsonPropertyName("conditionalOverride")]
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
}

/// <summary>
/// A conditional override for mode selection.
/// </summary>
public sealed record ModeSelectionOverride
{
  /// <summary>
  /// The condition that triggers the override.
  /// </summary>
  [JsonPropertyName("condition")]
  public required Condition Condition { get; init; }

  /// <summary>
  /// The mode selection to use when the condition is met.
  /// </summary>
  [JsonPropertyName("selection")]
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
  [JsonPropertyName("ability")]
  public required Ability Ability { get; init; }

  /// <summary>
  /// Optional name for the mode (used in some cards like Dawnbringer Cleric).
  /// e.g., "Cure Wounds", "Dispel Magic"
  /// </summary>
  [JsonPropertyName("name")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Name { get; init; }
}
