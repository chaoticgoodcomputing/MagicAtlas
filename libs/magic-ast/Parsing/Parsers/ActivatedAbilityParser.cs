namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// Parses activated abilities of the form "[Cost]: [Effect.]"
/// Handles:
/// - Mana abilities: {T}: Add {G}.
/// - Complex activated abilities: {3}{B}{B}: Creatures you control gain lifelink until end of turn.
/// - Loyalty abilities: +2: Discard up to two cards, then draw that many cards.
/// </summary>
public sealed partial class ActivatedAbilityParser
{
  private readonly ManaCostParser _manaCostParser = new();
  private readonly OracleTokenizer _tokenizer = new();

  /// <summary>
  /// Attempts to parse an activated ability from a clause.
  /// Returns null if parsing fails.
  /// </summary>
  public ActivatedAbility? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var text = clause.RawText;

    // Find the colon that separates cost from effect
    var colonIndex = text.IndexOf(':');
    if (colonIndex < 0)
    {
      return null;
    }

    // Split into cost and effect parts
    var costPart = text[..colonIndex].Trim();
    var effectPart = text[(colonIndex + 1)..].Trim();

    // Parse costs
    var costs = ParseCosts(costPart, classification);
    if (costs == null)
    {
      return null;
    }

    // Parse effects
    var effects = ParseEffects(effectPart);
    if (effects == null || effects.Count == 0)
    {
      return null;
    }

    // Determine if this is a mana ability
    var isManaAbility = IsManaAbility(costs, effects);

    // Build the activated ability
    return new ActivatedAbility
    {
      Costs = costs,
      Effects = effects,
      IsManaAbility = isManaAbility,
      LoyaltyCost = classification.LoyaltyCost,
      AbilityWord = classification.AbilityWord,
    };
  }

  /// <summary>
  /// Parses the cost portion of an activated ability.
  /// Returns null if parsing fails.
  /// </summary>
  private List<Cost>? ParseCosts(string costPart, ClauseClassification classification)
  {
    var costs = new List<Cost>();

    // Handle loyalty abilities (costs are empty, loyalty is tracked separately)
    if (classification.LoyaltyCost.HasValue)
    {
      return costs;
    }

    // Split by comma to get individual cost components
    var costComponents = costPart.Split(',').Select(c => c.Trim()).ToList();
    var hasParsedAnyCost = false;

    foreach (var component in costComponents)
    {
      // Try mana cost first (e.g., "{1}", "{2}{G}", "{T}")
      if (component.Contains('{'))
      {
        var manaCost = TryParseManaCostComponent(component);
        if (manaCost != null)
        {
          costs.Add(manaCost);
          hasParsedAnyCost = true;
          continue;
        }
      }

      // Try sacrifice cost (e.g., "Sacrifice another creature", "Sacrifice X Squirrels")
      var sacrificeCost = TryParseSacrificeCost(component);
      if (sacrificeCost != null)
      {
        costs.Add(sacrificeCost);
        hasParsedAnyCost = true;
        continue;
      }

      // Try discard cost (e.g., "Discard a card", "Discard a legendary card")
      var discardCost = TryParseDiscardCost(component);
      if (discardCost != null)
      {
        costs.Add(discardCost);
        hasParsedAnyCost = true;
        continue;
      }

      // If we can't parse this component, the whole cost parse fails
      // (We should be able to understand all cost components)
    }

    // If we couldn't parse any costs, return null to signal failure
    return hasParsedAnyCost ? costs : null;
  }

  /// <summary>
  /// Parses the effect portion of an activated ability.
  /// Returns null if parsing fails.
  /// </summary>
  private List<Effect>? ParseEffects(string effectPart)
  {
    // Try different effect types in sequence

    // Add mana
    var addManaEffect = TryParseAddManaEffect(effectPart);
    if (addManaEffect != null)
    {
      return new List<Effect> { addManaEffect };
    }

    // Scry
    var scryEffect = TryParseScryEffect(effectPart);
    if (scryEffect != null)
    {
      return new List<Effect> { scryEffect };
    }

    // Draw cards
    var drawEffect = TryParseDrawCardsEffect(effectPart);
    if (drawEffect != null)
    {
      return new List<Effect> { drawEffect };
    }

    // Discard cards
    var discardEffect = TryParseDiscardCardsEffect(effectPart);
    if (discardEffect != null)
    {
      return new List<Effect> { discardEffect };
    }

    // Put counters
    var putCounterEffect = TryParsePutCountersEffect(effectPart);
    if (putCounterEffect != null)
    {
      return new List<Effect> { putCounterEffect };
    }

    // Gain ability
    var gainAbilityEffect = TryParseGainAbilityEffect(effectPart);
    if (gainAbilityEffect != null)
    {
      return new List<Effect> { gainAbilityEffect };
    }

    // For now, we can't parse other effect types
    // Return null to signal that we need to fall back to unparsed
    return null;
  }

  /// <summary>
  /// Tries to parse "Add {mana}" effects.
  /// Pattern: "Add {G}", "Add {C}{C}{C}", "Add {W}{U}{B}{R}{G}", etc.
  /// </summary>
  private AddManaEffect? TryParseAddManaEffect(string effectText)
  {
    // Normalize whitespace
    effectText = effectText.Trim();

    // Pattern: "Add" followed by mana symbols, optionally ending with "."
    if (!effectText.StartsWith("Add ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    // Extract the mana portion (everything after "Add" and before optional ".")
    var manaText = effectText[4..].Trim();
    if (manaText.EndsWith('.'))
    {
      manaText = manaText[..^1].Trim();
    }

    // The mana text should be a sequence of mana symbols like "{G}" or "{C}{C}{C}"
    // We'll just pass it through as-is since AddManaEffect.Mana is a string
    if (string.IsNullOrWhiteSpace(manaText) || !manaText.Contains('{'))
    {
      return null;
    }

    return new AddManaEffect { Mana = manaText, AnyColor = false };
  }

  /// <summary>
  /// Determines if this is a mana ability (Rule 605).
  /// A mana ability:
  /// - Isn't a loyalty ability
  /// - Doesn't target
  /// - Could produce mana (has AddManaEffect)
  /// </summary>
  private static bool IsManaAbility(IReadOnlyList<Cost> costs, IReadOnlyList<Effect> effects)
  {
    // Check if any effect is an AddManaEffect
    var hasAddManaEffect = effects.Any(e => e is AddManaEffect);

    // For now, simple heuristic: if it adds mana and doesn't have complex targeting,
    // it's probably a mana ability
    return hasAddManaEffect;
  }

  /// <summary>
  /// Checks if a token kind represents a mana symbol.
  /// </summary>
  private static bool IsManaToken(OracleToken kind)
  {
    return kind == OracleToken.GenericMana
      || kind == OracleToken.VariableMana
      || kind == OracleToken.WhiteMana
      || kind == OracleToken.BlueMana
      || kind == OracleToken.BlackMana
      || kind == OracleToken.RedMana
      || kind == OracleToken.GreenMana
      || kind == OracleToken.ColorlessMana
      || kind == OracleToken.HybridMana
      || kind == OracleToken.PhyrexianMana
      || kind == OracleToken.TwoHybridMana
      || kind == OracleToken.HybridPhyrexianMana
      || kind == OracleToken.SnowMana;
  }

  /// <summary>
  /// Converts an OracleToken to a ManaSymbol.
  /// </summary>
  private ManaSymbol? ConvertTokenToManaSymbol(Token<OracleToken> token)
  {
    var content = token.ToStringValue().Trim('{', '}').ToUpperInvariant();

    // Use ManaCostParser to parse the symbol
    var parsed = _manaCostParser.Parse($"{{{content}}}");
    return parsed.Symbols.FirstOrDefault();
  }

  #region Cost Component Parsers

  /// <summary>
  /// Tries to parse a mana cost component like "{1}", "{2}{G}", "{T}", "{Q}".
  /// Returns ManaCost, TapCost, or UntapCost depending on the symbols.
  /// </summary>
  private Cost? TryParseManaCostComponent(string costText)
  {
    costText = costText.Trim();

    // Check for tap symbol
    if (costText == "{T}")
    {
      return new TapCost();
    }

    // Check for untap symbol
    if (costText == "{Q}")
    {
      return new UntapCost();
    }

    // Try to parse as mana cost using ManaCostParser
    try
    {
      var parsed = _manaCostParser.Parse(costText);
      if (parsed.Symbols.Count > 0)
      {
        return new ManaCost { Symbols = parsed.Symbols };
      }
    }
    catch
    {
      // Parsing failed, return null
    }

    return null;
  }

  /// <summary>
  /// Tries to parse sacrifice costs like "Sacrifice another creature", "Sacrifice X Squirrels".
  /// Reuses shared pattern logic with sacrifice effects.
  /// </summary>
  private SacrificeCost? TryParseSacrificeCost(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    if (!lower.StartsWith("sacrifice"))
    {
      return null;
    }

    // Parse using shared pattern helpers
    var (quantity, filter) = ParseSacrificePattern(costText);
    if (filter == null)
    {
      return null;
    }

    return new SacrificeCost { Filter = filter, Quantity = quantity };
  }

  /// <summary>
  /// Tries to parse discard costs like "Discard a card", "Discard a legendary card".
  /// Reuses shared pattern logic with discard effects.
  /// </summary>
  private DiscardCost? TryParseDiscardCost(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    if (!lower.StartsWith("discard"))
    {
      return null;
    }

    // Parse using shared pattern helpers
    var (quantity, filter) = ParseDiscardPattern(costText);

    return new DiscardCost { Filter = filter, Quantity = quantity };
  }

  #endregion

  #region Shared Pattern Parsers (used by both costs and effects)

  /// <summary>
  /// Parses "sacrifice [quantity] [filter]" patterns.
  /// Returns (quantity, filter) tuple that can be used for both costs and effects.
  /// </summary>
  private (Quantity quantity, ObjectFilter? filter) ParseSacrificePattern(string text)
  {
    var lower = text.ToLowerInvariant();

    // Parse quantity
    Quantity quantity;
    if (lower.Contains(" x "))
    {
      quantity = VariableQuantity.X;
    }
    else
    {
      var count = ParseNumberWord(text) ?? 1;
      quantity = LiteralQuantity.Of(count);
    }

    // Parse filter
    ObjectFilter? filter = null;
    if (lower.Contains("another creature"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"], Characteristics = ["another"] };
    }
    else if (lower.Contains("this creature") || lower.Contains("this permanent"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"] };
    }
    else if (lower.Contains("creature"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"] };
    }
    else if (lower.Contains("artifact"))
    {
      filter = new ObjectFilter { CardTypes = ["artifact"] };
    }
    else
    {
      // Try to extract the type from the text
      // Pattern: "Sacrifice [count] [type]"
      var match = Regex.Match(
        text,
        @"(?:Sacrifice|sacrifice) (?:a |an |X )?(\w+)",
        RegexOptions.IgnoreCase
      );
      if (match.Success)
      {
        var type = match.Groups[1].Value.ToLowerInvariant();
        // Handle plurals (e.g., "Squirrels" -> "Squirrel")
        if (type.EndsWith("s") && type != "this")
        {
          type = type[..^1];
        }
        filter = new ObjectFilter { Subtypes = [type] };
      }
    }

    return (quantity, filter);
  }

  /// <summary>
  /// Parses "discard [quantity] [filter]" patterns.
  /// Returns (quantity, filter) tuple that can be used for both costs and effects.
  /// </summary>
  private (Quantity quantity, ObjectFilter filter) ParseDiscardPattern(string text)
  {
    var lower = text.ToLowerInvariant();

    // Parse quantity
    var count = ParseNumberWord(text) ?? 1;
    var quantity = LiteralQuantity.Of(count);

    // Parse filter
    ObjectFilter filter;
    if (lower.Contains("legendary card"))
    {
      filter = new ObjectFilter { Supertypes = ["Legendary"], CardTypes = ["card"] };
    }
    else
    {
      filter = new ObjectFilter { CardTypes = ["card"] };
    }

    return (quantity, filter);
  }

  #endregion

  #region Effect Parsers

  /// <summary>
  /// Tries to parse "Scry N" effects.
  /// Pattern: "Scry 2", "Scry 1", etc.
  /// </summary>
  private ScryEffect? TryParseScryEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');

    var match = Regex.Match(effectText, @"^Scry\s+(\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return null;
    }

    var count = int.Parse(match.Groups[1].Value);
    return new ScryEffect { Count = LiteralQuantity.Of(count) };
  }

  /// <summary>
  /// Tries to parse "Draw N cards" effects.
  /// Patterns: "Draw two cards", "Draw a card", "Each other player draws a card"
  /// </summary>
  private DrawCardsEffect? TryParseDrawCardsEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    // Pattern: "draw [count] card(s)"
    if (!lower.Contains("draw"))
    {
      return null;
    }

    // Determine player
    ObjectReference player;
    if (lower.Contains("each other player"))
    {
      player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent };
    }
    else if (lower.Contains("you"))
    {
      player = ObjectReference.You();
    }
    else
    {
      // Default to "you"
      player = ObjectReference.You();
    }

    // Parse count
    var count = ParseNumberWord(effectText) ?? 1;

    return new DrawCardsEffect { Count = LiteralQuantity.Of(count), Player = player };
  }

  /// <summary>
  /// Tries to parse "Discard N cards" effects.
  /// Patterns: "Discard up to two cards", "Discard a legendary card"
  /// </summary>
  private DiscardCardsEffect? TryParseDiscardCardsEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("discard"))
    {
      return null;
    }

    // Parse "up to N"
    var upToMatch = Regex.Match(effectText, @"up to (\w+)", RegexOptions.IgnoreCase);
    int count;
    if (upToMatch.Success)
    {
      count = ParseNumberWord(upToMatch.Groups[1].Value) ?? 1;
    }
    else
    {
      count = ParseNumberWord(effectText) ?? 1;
    }

    // Check for filter (e.g., "a legendary card")
    ObjectFilter? filter = null;
    if (lower.Contains("legendary"))
    {
      filter = new ObjectFilter { Supertypes = ["legendary"] };
    }

    return new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
      Filter = filter,
      Random = false,
    };
  }

  /// <summary>
  /// Tries to parse "Put N +1/+1 counters on [target]" effects.
  /// Patterns: "Put a +1/+1 counter on this creature", "Put a +1/+1 counter on target creature you control"
  /// </summary>
  private PutCountersEffect? TryParsePutCountersEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("put") || !lower.Contains("counter"))
    {
      return null;
    }

    // Parse counter type
    string counterType;
    if (lower.Contains("+1/+1"))
    {
      counterType = "+1/+1";
    }
    else if (lower.Contains("-1/-1"))
    {
      counterType = "-1/-1";
    }
    else
    {
      return null; // Unknown counter type
    }

    // Parse count
    var count = ParseNumberWord(effectText) ?? 1;

    // Parse target
    ObjectReference target;
    if (lower.Contains("this creature") || lower.Contains("this permanent"))
    {
      target = ObjectReference.Self();
    }
    else if (lower.Contains("target creature you control"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      };
    }
    else if (lower.Contains("target creature"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      };
    }
    else
    {
      // Default to self
      target = ObjectReference.Self();
    }

    return new PutCountersEffect
    {
      Target = target,
      CounterType = counterType,
      Count = LiteralQuantity.Of(count),
    };
  }

  /// <summary>
  /// Tries to parse "Creatures you control gain [ability] until end of turn" effects.
  /// Pattern: "Creatures you control gain lifelink until end of turn"
  /// </summary>
  private GainAbilityEffect? TryParseGainAbilityEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("gain"))
    {
      return null;
    }

    // Pattern: "Creatures you control gain [ability]"
    var match = Regex.Match(
      effectText,
      @"Creatures you control gain (\w+)",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var ability = match.Groups[1].Value;

    return new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      },
      AbilityText = ability,
    };
  }

  /// <summary>
  /// Parses number words like "one", "two", "three" into integers.
  /// Returns null if no number word is found.
  /// </summary>
  private int? ParseNumberWord(string text)
  {
    var lower = text.ToLowerInvariant();

    if (lower.Contains("two"))
      return 2;
    if (lower.Contains("three"))
      return 3;
    if (lower.Contains("four"))
      return 4;
    if (lower.Contains("five"))
      return 5;
    if (lower.Contains("six"))
      return 6;
    if (lower.Contains("seven"))
      return 7;
    if (lower.Contains("eight"))
      return 8;
    if (lower.Contains("nine"))
      return 9;
    if (lower.Contains("ten"))
      return 10;
    if (lower.Contains("one") || lower.Contains(" a ") || lower.Contains("an "))
      return 1;

    // Try to find a digit
    var digitMatch = Regex.Match(text, @"\b(\d+)\b");
    if (digitMatch.Success)
    {
      return int.Parse(digitMatch.Groups[1].Value);
    }

    return null;
  }

  #endregion
}
