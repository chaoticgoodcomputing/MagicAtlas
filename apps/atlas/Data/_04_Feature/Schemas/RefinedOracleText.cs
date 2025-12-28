using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._04_Feature.Schemas;

/// <summary>
/// Opinionated core data for a Magic: The Gathering card. This represents data that I've
/// personally determined might be material for analysis.
/// </summary>
public record RefinedOracleText : IStructuredSerializable
{
  // =====================
  // MARK: IDENTIFIERS
  // =====================

  /// <summary>
  /// A unique ID for this card in Scryfall's database.
  /// </summary>
  public required Guid Id { get; init; }

  // =====================
  // MARK: CONTENT
  // =====================

  /// <summary>
  /// The name of this card.
  /// </summary>
  /// <remarks>
  /// If this card has multiple faces, this field will contain both names separated by ␣//␣.
  /// </remarks>
  public required string Name { get; init; }

  /// <summary>
  /// Full oracle text for the card.
  /// </summary>
  public required string RawText { get; init; }

  /// <summary>
  /// Full oracle text for the card.
  /// </summary>
  public required string RefinedText { get; init; }

  /// <summary>
  /// List of single-word keyword abilities found in the oracle text.
  ///
  /// For example: "Flying", "Vigilance, trample, haste", "Firebending 1"
  /// </summary>
  public List<KeywordAbility> KeywordAbilities { get; init; } = new();

  /// <summary>
  /// List of named triggered abilities found in the oracle text.
  ///
  /// For example, "Landfall — Whenever a land you control enters, you may exile target nonland
  /// permanent other than this creature."
  /// </summary>
  public List<NamedTriggeredAbility> NamedTriggeredAbilities { get; init; } = new();

  /// <summary>
  /// List of activated abilities found in the oracle text.
  ///
  /// For example: "{2}, {T}, Sacrifice an artifact: You get {E}{E} and draw a card.
  /// </summary>
  public List<ActivatedAbility> ActivatedAbilities { get; init; } = new();

  /// <summary>
  /// List of triggered abilities found in the oracle text.
  ///
  /// For example: "When Greta enters, create a Food token."
  /// </summary>
  public List<TriggeredAbility> TriggeredAbilities { get; init; } = new();

  /// <summary>
  /// List of passive abilities found in the oracle text. These are any abilities that are not
  /// keyword, activated, or triggered abilities.
  ///
  /// Technically, includes spell abilities and static abilities.
  /// </summary>
  public List<PassiveAbility> PassiveAbilities { get; init; } = new();
}

/// <summary>
/// A passive ability found in a card's oracle text.
/// </summary>
public class PassiveAbility
{
  public required string Effect { get; init; }
}

/// <summary>
/// An activated ability found in a card's oracle text.
/// </summary>
public class ActivatedAbility
{
  public required string RawText { get; init; }
  public List<string> Costs { get; init; } = new();
  public required string Effect { get; init; }
}

/// <summary>
/// A triggered ability found in a card's oracle text.
/// </summary>
public class TriggeredAbility
{
  public required string RawText { get; init; }
  public required string Trigger { get; init; }
  public required string Effect { get; init; }
}

/// <summary>
/// A single-word keyword ability found in a card's oracle text.
/// </summary>
public class KeywordAbility
{
  public required string RawText { get; init; }
}

/// <summary>
/// A named triggered ability found in a card's oracle text.
/// </summary>
public class NamedTriggeredAbility
{
  public required string RawText { get; init; }
  public required string Keyword { get; init; }
  public required string Effect { get; init; }
}
