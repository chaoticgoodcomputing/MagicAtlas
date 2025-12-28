namespace MagicAST;

using System.Text.Json.Serialization;

/// <summary>
/// Data transfer object representing a card's raw data as input to the parser.
/// This matches the input contract from the architecture document.
/// </summary>
public sealed record CardInputDTO
{
  /// <summary>
  /// The card's name.
  /// Example: "Chatterfang, Squirrel General"
  /// </summary>
  [JsonPropertyName("name")]
  public required string Name { get; init; }

  /// <summary>
  /// The card's mana cost in symbol notation.
  /// Example: "{1}{G}{G}"
  /// </summary>
  [JsonPropertyName("manaCost")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ManaCost { get; init; }

  /// <summary>
  /// The card's type line.
  /// Example: "Legendary Creature — Squirrel Warrior"
  /// </summary>
  [JsonPropertyName("typeLine")]
  public required string TypeLine { get; init; }

  /// <summary>
  /// The oracle text containing all abilities.
  /// Abilities are separated by paragraph breaks (\n).
  /// This is the primary parsing target.
  /// </summary>
  [JsonPropertyName("oracleText")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? OracleText { get; init; }

  /// <summary>
  /// The card's power (for creatures).
  /// May contain non-numeric values like "*" or "1+*".
  /// </summary>
  [JsonPropertyName("power")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Power { get; init; }

  /// <summary>
  /// The card's toughness (for creatures).
  /// May contain non-numeric values like "*" or "1+*".
  /// </summary>
  [JsonPropertyName("toughness")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Toughness { get; init; }

  /// <summary>
  /// The card's starting loyalty (for planeswalkers).
  /// </summary>
  [JsonPropertyName("loyalty")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Loyalty { get; init; }

  /// <summary>
  /// The card's colors.
  /// Example: ["G"] or ["W", "U"]
  /// </summary>
  [JsonPropertyName("colors")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Colors { get; init; }

  /// <summary>
  /// The card's color identity (for Commander format).
  /// Includes all colors in mana cost and rules text.
  /// Example: ["G"] or ["W", "U", "B"]
  /// </summary>
  [JsonPropertyName("colorIdentity")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? ColorIdentity { get; init; }

  /// <summary>
  /// Keywords explicitly identified on the card by the data source.
  /// These may be used to assist parsing or validate results.
  /// Example: ["Forestwalk", "Flying"]
  /// </summary>
  [JsonPropertyName("keywords")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Keywords { get; init; }

  /// <summary>
  /// The card's unique identifier from the source system.
  /// </summary>
  [JsonPropertyName("id")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Id { get; init; }

  /// <summary>
  /// Card layout type (normal, split, flip, transform, etc.)
  /// </summary>
  [JsonPropertyName("layout")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Layout { get; init; }

  /// <summary>
  /// For multi-faced cards (split, transform, etc.), the individual faces.
  /// </summary>
  [JsonPropertyName("cardFaces")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<CardFaceDTO>? CardFaces { get; init; }
}

/// <summary>
/// Represents one face of a multi-faced card.
/// </summary>
public sealed record CardFaceDTO
{
  /// <summary>
  /// The face's name.
  /// </summary>
  [JsonPropertyName("name")]
  public required string Name { get; init; }

  /// <summary>
  /// The face's mana cost.
  /// </summary>
  [JsonPropertyName("manaCost")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ManaCost { get; init; }

  /// <summary>
  /// The face's type line.
  /// </summary>
  [JsonPropertyName("typeLine")]
  public required string TypeLine { get; init; }

  /// <summary>
  /// The face's oracle text.
  /// </summary>
  [JsonPropertyName("oracleText")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? OracleText { get; init; }

  /// <summary>
  /// The face's power (for creature faces).
  /// </summary>
  [JsonPropertyName("power")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Power { get; init; }

  /// <summary>
  /// The face's toughness (for creature faces).
  /// </summary>
  [JsonPropertyName("toughness")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Toughness { get; init; }

  /// <summary>
  /// The face's loyalty (for planeswalker faces).
  /// </summary>
  [JsonPropertyName("loyalty")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Loyalty { get; init; }

  /// <summary>
  /// The face's colors.
  /// </summary>
  [JsonPropertyName("colors")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Colors { get; init; }
}
