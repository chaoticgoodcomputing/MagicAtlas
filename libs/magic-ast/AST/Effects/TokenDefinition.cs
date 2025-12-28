namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;

/// <summary>
/// Defines a token that can be created.
/// </summary>
public sealed record TokenDefinition
{
  /// <summary>
  /// Power of the token (for creatures).
  /// </summary>
  [JsonPropertyName("power")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Power { get; init; }

  /// <summary>
  /// Toughness of the token (for creatures).
  /// </summary>
  [JsonPropertyName("toughness")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Toughness { get; init; }

  /// <summary>
  /// Colors of the token.
  /// </summary>
  [JsonPropertyName("colors")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Colors { get; init; }

  /// <summary>
  /// Card types of the token.
  /// </summary>
  [JsonPropertyName("types")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Types { get; init; }

  /// <summary>
  /// Subtypes of the token.
  /// </summary>
  [JsonPropertyName("subtypes")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }

  /// <summary>
  /// Name of the token if specified.
  /// </summary>
  [JsonPropertyName("name")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Name { get; init; }

  /// <summary>
  /// Abilities the token has.
  /// </summary>
  [JsonPropertyName("abilities")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<Ability>? Abilities { get; init; }

  /// <summary>
  /// Raw ability text if abilities aren't fully parsed.
  /// </summary>
  [JsonPropertyName("abilityText")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? AbilityText { get; init; }

  /// <summary>
  /// True if this is a copy of another object.
  /// </summary>
  [JsonPropertyName("isCopy")]
  public bool IsCopy { get; init; }

  // Factory methods for common tokens
  public static TokenDefinition Treasure() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Treasure"],
      AbilityText = ["{T}, Sacrifice this artifact: Add one mana of any color."],
    };

  public static TokenDefinition Food() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Food"],
      AbilityText = ["{2}, {T}, Sacrifice this artifact: You gain 3 life."],
    };

  public static TokenDefinition Clue() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Clue"],
      AbilityText = ["{2}, Sacrifice this artifact: Draw a card."],
    };

  public static TokenDefinition Blood() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Blood"],
      AbilityText = ["{1}, {T}, Discard a card, Sacrifice this artifact: Draw a card."],
    };
}
