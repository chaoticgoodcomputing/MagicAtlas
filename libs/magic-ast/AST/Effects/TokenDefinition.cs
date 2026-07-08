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
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Power { get; init; }

  /// <summary>
  /// Toughness of the token (for creatures).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Toughness { get; init; }

  /// <summary>
  /// Colors of the token.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Colors { get; init; }

  /// <summary>
  /// Card types of the token.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Types { get; init; }

  /// <summary>
  /// Subtypes of the token.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }

  /// <summary>
  /// Name of the token if specified.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Name { get; init; }

  /// <summary>
  /// Abilities the token has.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<Ability>? Abilities { get; init; }

  /// <summary>
  /// Raw ability text if abilities aren't fully parsed.
  /// </summary>
  [FreeTextField]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? AbilityText { get; init; }

  /// <summary>
  /// True if this is a copy of another object.
  /// </summary>
  public bool IsCopy { get; init; }

  /// <summary>
  /// True when the token enters the battlefield tapped (e.g. "create a tapped Treasure token" —
  /// CR 110.6b: "A token enters the battlefield under the control of the player who created it."
  /// CR 305.9 / 302.6: a permanent can be instructed to enter tapped by an effect). Null (omitted
  /// in JSON) when the token enters normally (untapped). Distinct from the permanent's current
  /// tapped state at resolution: this is a declarative entry modifier, not a tap action.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? EntersTapped { get; init; }

  // Factory methods for common tokens
  public static TokenDefinition Treasure() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Treasure"],
      AbilityText = ["{T}, Sacrifice this artifact: Add one mana of any color."],
    };

  public static TokenDefinition TappedTreasure() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Treasure"],
      AbilityText = ["{T}, Sacrifice this artifact: Add one mana of any color."],
      EntersTapped = true,
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

  /// <summary>
  /// A Map token — a colorless artifact predefined token (CR 111.10 — predefined tokens).
  /// Its predefined activated ability ("{1}, {T}, Sacrifice this token: Target creature you
  /// control explores. Activate only as a sorcery.") rides on the parenthetical reminder text
  /// the parser strips (CR 207.2 — reminder text has no rules meaning), so — following the
  /// Powerstone precedent added clean in <c>CreateTappedPredefinedTokenRule</c> — the ability
  /// body is NOT re-asserted here as free text. The token is identified structurally by its
  /// artifact type + named "Map" subtype.
  /// </summary>
  public static TokenDefinition Map() =>
    new()
    {
      Types = ["artifact"],
      Subtypes = ["Map"],
    };
}
