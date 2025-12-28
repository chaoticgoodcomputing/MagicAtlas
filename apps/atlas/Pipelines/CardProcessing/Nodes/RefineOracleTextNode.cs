using System.Text.RegularExpressions;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._04_Feature.Schemas;

namespace MagicAtlas.Pipelines.CardProcessing.Nodes;

/// <summary>
/// Processes card oracle text by removing parentheticals and categorizing abilities.
/// </summary>
public static class RefineOracleTextNode
{
  private static readonly Regex _parentheticalPattern = new(@"\s*\([^)]*\)", RegexOptions.Compiled);
  private static readonly Regex _keywordPattern =
    new(
      @"^\w+(?:\s+(?:\d+|\{[^}]+\}))?(?:,\s+\w+(?:\s+(?:\d+|\{[^}]+\}))?)*$",
      RegexOptions.Compiled
    );

  /// <summary>
  /// Creates an oracle text refinement function that processes card text and abilities.
  /// </summary>
  /// <returns>
  /// A function that takes card core data and produces refined oracle text
  /// with parentheticals removed and categorized abilities.
  /// </returns>
  public static Func<IEnumerable<CardCoreData>, Task<IEnumerable<RefinedOracleText>>> Create()
  {
    return async (cards) =>
    {
      var refined = cards
        .Where(card => !string.IsNullOrWhiteSpace(card.OracleText))
        .Select(card => RefineCard(card))
        .ToList();

      return await Task.FromResult(refined);
    };
  }

  /// <summary>
  /// Refines a single card's oracle text.
  /// </summary>
  private static RefinedOracleText RefineCard(CardCoreData card)
  {
    var rawText = card.OracleText ?? "";
    var refinedText = RemoveParentheticals(rawText);
    var lines = refinedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    var keywordAbilities = new List<KeywordAbility>();
    var namedTriggeredAbilities = new List<NamedTriggeredAbility>();
    var triggeredAbilities = new List<TriggeredAbility>();
    var activatedAbilities = new List<ActivatedAbility>();
    var passiveAbilities = new List<PassiveAbility>();

    foreach (var line in lines)
    {
      var trimmedLine = line.Trim();
      if (string.IsNullOrWhiteSpace(trimmedLine))
      {
        continue;
      }

      // Check for single-word keyword ability (e.g., "Flying", "Vigilance, trample")
      if (IsKeywordAbility(trimmedLine))
      {
        keywordAbilities.Add(new KeywordAbility { RawText = trimmedLine });
      }
      // Check for named triggered ability (contains em dash —)
      else if (IsNamedTriggeredAbility(trimmedLine))
      {
        namedTriggeredAbilities.Add(ParseNamedTriggeredAbility(trimmedLine));
      }
      // Check for triggered ability (starts with "When", "Whenever", or "At")
      else if (IsTriggeredAbility(trimmedLine))
      {
        triggeredAbilities.Add(ParseTriggeredAbility(trimmedLine));
      }
      // Check for activated ability (contains colon before any opening parenthesis)
      else if (IsActivatedAbility(trimmedLine))
      {
        activatedAbilities.Add(ParseActivatedAbility(trimmedLine));
      }
      // Otherwise, it's a passive ability
      else
      {
        passiveAbilities.Add(new PassiveAbility { Effect = trimmedLine });
      }
    }

    return new RefinedOracleText
    {
      Id = card.Id,
      Name = card.Name,
      RawText = rawText,
      RefinedText = refinedText,
      KeywordAbilities = keywordAbilities,
      NamedTriggeredAbilities = namedTriggeredAbilities,
      TriggeredAbilities = triggeredAbilities,
      ActivatedAbilities = activatedAbilities,
      PassiveAbilities = passiveAbilities,
    };
  }

  /// <summary>
  /// Removes parenthetical text from a string.
  /// </summary>
  private static string RemoveParentheticals(string text)
  {
    return _parentheticalPattern.Replace(text, "").Trim();
  }

  /// <summary>
  /// Checks if a line represents a keyword ability.
  /// A keyword ability matches single words separated by ", " with optional numbers.
  /// Examples: "Flying", "Vigilance, trample", "Firebending 1", "Flying, vigilance, deathtouch"
  /// </summary>
  private static bool IsKeywordAbility(string line)
  {
    return _keywordPattern.IsMatch(line);
  }

  /// <summary>
  /// Checks if a line represents a named triggered ability.
  /// A named triggered ability contains " - " (space-hyphen-space).
  /// Note: Em-dashes are normalized to hyphens by TextNormalizer.
  /// </summary>
  private static bool IsNamedTriggeredAbility(string line)
  {
    var dashIndex = line.IndexOf(" - ");
    if (dashIndex == -1)
    {
      return false;
    }

    // Ensure there's content before and after the dash
    return dashIndex > 0 && dashIndex + 3 < line.Length;
  }

  /// <summary>
  /// Checks if a line represents a triggered ability.
  /// A triggered ability starts with "When", "Whenever", or "At".
  /// </summary>
  private static bool IsTriggeredAbility(string line)
  {
    return line.StartsWith("When ", StringComparison.Ordinal)
      || line.StartsWith("Whenever ", StringComparison.Ordinal)
      || line.StartsWith("At ", StringComparison.Ordinal);
  }

  /// <summary>
  /// Checks if a line represents an activated ability.
  /// An activated ability has a colon that appears before any opening parenthesis.
  /// </summary>
  private static bool IsActivatedAbility(string line)
  {
    var colonIndex = line.IndexOf(':');
    if (colonIndex == -1)
    {
      return false;
    }

    var parenIndex = line.IndexOf('(');
    // If there's no paren, or the colon comes before the paren, it's an activated ability
    return parenIndex == -1 || colonIndex < parenIndex;
  }

  /// <summary>
  /// Parses a named triggered ability line (format: "Keyword — Effect").
  /// </summary>
  private static NamedTriggeredAbility ParseNamedTriggeredAbility(string line)
  {
    var parts = line.Split(" — ", 2, StringSplitOptions.TrimEntries);
    return new NamedTriggeredAbility
    {
      RawText = line,
      Keyword = parts.Length > 0 ? parts[0] : "",
      Effect = parts.Length > 1 ? parts[1] : "",
    };
  }

  /// <summary>
  /// Parses a triggered ability line (format: "When/Whenever/At [trigger], [effect]").
  /// </summary>
  private static TriggeredAbility ParseTriggeredAbility(string line)
  {
    var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
    return new TriggeredAbility
    {
      RawText = line,
      Trigger = parts.Length > 0 ? parts[0] : "",
      Effect = parts.Length > 1 ? parts[1] : "",
    };
  }

  /// <summary>
  /// Parses an activated ability line (format: "Cost1, Cost2, Cost3: Effect").
  /// </summary>
  private static ActivatedAbility ParseActivatedAbility(string line)
  {
    var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
    var costString = parts.Length > 0 ? parts[0] : "";
    var effect = parts.Length > 1 ? parts[1] : "";

    // Split costs by ", " and trim each
    var costs = costString
      .Split(", ", StringSplitOptions.RemoveEmptyEntries)
      .Select(c => c.Trim())
      .ToList();

    return new ActivatedAbility
    {
      RawText = line,
      Costs = costs,
      Effect = effect,
    };
  }
}
