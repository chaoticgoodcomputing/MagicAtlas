namespace MagicAST.Parsing.Parsers;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;

/// <summary>
/// Parser for triggered abilities: "When/Whenever/At [trigger], [effect]"
/// Supports multiple trigger events and effect types using compositional parsing.
/// </summary>
/// <remarks>
/// This parser uses a compositional approach to handle families of similar cards
/// rather than overfitting to specific card text. Components are designed to be
/// reusable across different trigger and effect patterns.
/// </remarks>
public sealed class TriggeredAbilityParser
{
  /// <summary>
  /// Attempts to parse a triggered ability from a clause.
  /// </summary>
  /// <param name="clause">The clause to parse.</param>
  /// <param name="classification">The classification information.</param>
  /// <returns>A parsed TriggeredAbility or null if parsing fails.</returns>
  public TriggeredAbility? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var text = clause.RawText;
    var tokens = clause.Tokens.ToList();

    if (tokens.Count == 0)
    {
      return null;
    }

    // Parse trigger timing (When/Whenever/At)
    var triggerTiming = ParseTriggerTiming(tokens[0].Kind);
    if (triggerTiming == null)
    {
      return null;
    }

    // Split into trigger and effect parts at comma
    var parts = SplitTriggerAndEffect(text);
    if (parts == null)
    {
      return null;
    }

    var (triggerPart, effectPart) = parts.Value;

    // Parse trigger event and filter
    var trigger = ParseTriggerCondition(triggerPart, triggerTiming.Value);
    if (trigger == null)
    {
      return null;
    }

    // Parse effects
    var effects = ParseEffects(effectPart);
    if (effects == null || effects.Count == 0)
    {
      return null;
    }

    return new TriggeredAbility { Trigger = trigger, Effects = effects };
  }

  #region Trigger Parsing

  /// <summary>
  /// Parses the trigger timing keyword.
  /// </summary>
  private static TriggerTiming? ParseTriggerTiming(OracleToken token) =>
    token switch
    {
      OracleToken.When => TriggerTiming.When,
      OracleToken.Whenever => TriggerTiming.Whenever,
      OracleToken.At => TriggerTiming.At,
      _ => null,
    };

  /// <summary>
  /// Splits the clause into trigger and effect parts at the first comma.
  /// </summary>
  private static (string Trigger, string Effect)? SplitTriggerAndEffect(string text)
  {
    var parts = text.Split(',', 2);
    if (parts.Length != 2)
    {
      return null;
    }

    return (parts[0].Trim(), parts[1].Trim());
  }

  /// <summary>
  /// Parses the trigger condition (event + filter).
  /// Dispatches to specific event parsers based on keywords.
  /// </summary>
  private static TriggerCondition? ParseTriggerCondition(string triggerText, TriggerTiming timing)
  {
    var lower = triggerText.ToLowerInvariant();

    // Try different trigger event types
    if (lower.Contains("dies"))
    {
      return ParseDiesTrigger(triggerText, timing);
    }

    if (lower.Contains("enters"))
    {
      return ParseEntersTrigger(triggerText, timing);
    }

    // Add more trigger types here as needed (attacks, etc.)

    return null;
  }

  /// <summary>
  /// Parses "dies" triggers.
  /// Supports: "this creature dies", "a creature dies", "another creature dies", etc.
  /// </summary>
  private static TriggerCondition? ParseDiesTrigger(string triggerText, TriggerTiming timing)
  {
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = filter,
    };
  }

  /// <summary>
  /// Parses "enters" triggers.
  /// Supports: "this creature enters", "a creature enters", etc.
  /// </summary>
  private static TriggerCondition? ParseEntersTrigger(string triggerText, TriggerTiming timing)
  {
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Enters,
      Filter = filter,
    };
  }

  /// <summary>
  /// Parses object filters from trigger text.
  /// Compositional component used by multiple trigger types.
  /// </summary>
  private static ObjectFilter? ParseObjectFilter(string text)
  {
    var lower = text.ToLowerInvariant();

    // Simple filters - can be extended with more sophisticated parsing
    if (lower.Contains("this creature"))
    {
      return new ObjectFilter { CardTypes = ["creature"] };
    }

    if (lower.Contains("a creature") || lower.Contains("another creature"))
    {
      return new ObjectFilter { CardTypes = ["creature"] };
    }

    return null;
  }

  #endregion

  #region Effect Parsing

  /// <summary>
  /// Parses the effects portion of the triggered ability.
  /// Tries different effect parsers in sequence.
  /// </summary>
  private static IReadOnlyList<Effect>? ParseEffects(string effectText)
  {
    // Try different effect types
    var effect = TryParseCreateTokenEffect(effectText);
    if (effect != null)
    {
      return new List<Effect> { effect };
    }

    // Can add more effect types here:
    // - ParseDrawEffect(effectText)
    // - ParseGainLifeEffect(effectText)
    // - ParsePutCounterEffect(effectText)

    return null;
  }

  /// <summary>
  /// Parses "create [article] [P/T] [colors] [subtypes] creature token [abilities]" patterns.
  /// Supports variations like:
  /// - "create a 1/1 green Saproling creature token"
  /// - "create two 2/2 black Zombie creature tokens"
  /// - "create a 1/1 white and black Spirit creature token with flying"
  /// </summary>
  private static CreateTokenEffect? TryParseCreateTokenEffect(string text)
  {
    if (!text.Contains("create", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    // Parse article/quantity
    var (article, count) = ParseArticle(text);

    // Parse power/toughness (P/T)
    var powerToughness = ParsePowerToughness(text);
    if (powerToughness == null)
    {
      return null;
    }

    // Parse colors
    var colors = ParseColors(text);

    // Parse creature subtypes
    var subtypes = ParseCreatureSubtypes(text);
    if (subtypes.Count == 0)
    {
      return null;
    }

    // Parse abilities (optional)
    var abilities = ParseTokenAbilities(text);

    return new CreateTokenEffect
    {
      Count = LiteralQuantity.Of(count),
      Token = new TokenDefinition
      {
        Power = powerToughness.Value.Power,
        Toughness = powerToughness.Value.Toughness,
        Colors = colors,
        Types = ["creature"],
        Subtypes = subtypes,
        // Abilities would be stored here if TokenDefinition supported them
      },
    };
  }

  #endregion

  #region Token Spec Parsing Components

  /// <summary>
  /// Parses article/quantity from token creation text.
  /// Compositional component for token parsing.
  /// </summary>
  private static (string Article, int Count) ParseArticle(string text)
  {
    var lower = text.ToLowerInvariant();

    if (lower.Contains("two "))
    {
      return ("two", 2);
    }

    if (lower.Contains("three "))
    {
      return ("three", 3);
    }

    if (lower.Contains("four "))
    {
      return ("four", 4);
    }

    if (lower.Contains("an "))
    {
      return ("an", 1);
    }

    if (lower.Contains("a "))
    {
      return ("a", 1);
    }

    return ("", 1); // Default to 1
  }

  /// <summary>
  /// Parses power/toughness notation (e.g., "1/1", "2/2", "X/X").
  /// Compositional component reusable across parsers.
  /// </summary>
  private static (string Power, string Toughness)? ParsePowerToughness(string text)
  {
    // Match N/N pattern where N is digit or X
    var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+|X)/(\d+|X)");
    if (!match.Success)
    {
      return null;
    }

    return (match.Groups[1].Value, match.Groups[2].Value);
  }

  /// <summary>
  /// Parses color words from text.
  /// Handles single colors and compound colors (e.g., "white and black").
  /// Compositional component reusable across parsers.
  /// </summary>
  private static List<string> ParseColors(string text)
  {
    var colors = new List<string>();
    var lower = text.ToLowerInvariant();

    // Map color names to abbreviations
    var colorMappings = new Dictionary<string, string>
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

    foreach (var (name, code) in colorMappings)
    {
      if (lower.Contains(name))
      {
        colors.Add(code);
      }
    }

    // Handle colorless explicitly
    if (lower.Contains("colorless"))
    {
      colors.Clear();
      colors.Add("C");
    }

    return colors;
  }

  /// <summary>
  /// Parses creature subtypes from token creation text.
  /// Looks for common creature types between P/T and "creature token".
  /// Compositional component for token parsing.
  /// </summary>
  private static List<string> ParseCreatureSubtypes(string text)
  {
    var subtypes = new List<string>();

    // Common creature types from data analysis
    var knownTypes = new[]
    {
      "Saproling",
      "Zombie",
      "Spirit",
      "Goblin",
      "Soldier",
      "Human",
      "Elf",
      "Warrior",
      "Wolf",
      "Dragon",
      "Thopter",
      "Servo",
      "Knight",
      "Vampire",
      "Cat",
      "Bat",
      "Bird",
      "Insect",
      "Squirrel",
      "Citizen",
      "Rat",
      "Angel",
      "Detective",
      "Robot",
      "Kraken",
      "Eldrazi",
      "Phyrexian",
      "Germ",
      "Golem",
      "Rebel",
      "Hero",
      "Ally",
      "Orc",
      "Army",
    };

    foreach (var type in knownTypes)
    {
      if (text.Contains(type, StringComparison.OrdinalIgnoreCase))
      {
        subtypes.Add(type);
      }
    }

    return subtypes;
  }

  /// <summary>
  /// Parses token abilities (e.g., "with flying", "with lifelink").
  /// Compositional component for token parsing.
  /// </summary>
  private static List<string> ParseTokenAbilities(string text)
  {
    var abilities = new List<string>();
    var lower = text.ToLowerInvariant();

    // Common token abilities
    if (lower.Contains("with flying"))
    {
      abilities.Add("flying");
    }

    if (lower.Contains("with lifelink"))
    {
      abilities.Add("lifelink");
    }

    if (lower.Contains("with vigilance"))
    {
      abilities.Add("vigilance");
    }

    if (lower.Contains("with deathtouch"))
    {
      abilities.Add("deathtouch");
    }

    if (lower.Contains("with haste"))
    {
      abilities.Add("haste");
    }

    if (lower.Contains("with trample"))
    {
      abilities.Add("trample");
    }

    return abilities;
  }

  #endregion
}
