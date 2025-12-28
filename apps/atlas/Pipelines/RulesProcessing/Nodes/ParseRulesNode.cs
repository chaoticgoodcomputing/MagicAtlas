using System.Text.RegularExpressions;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Helpers;

namespace MagicAtlas.Pipelines.RulesProcessing.Nodes;

/// <summary>
/// Parses rules text into hierarchical structure.
/// </summary>
public static partial class ParseRulesNode
{
  public static Func<string, Task<RulesStructure>> Create()
  {
    return async (rulesText) =>
    {
      var structure = new RulesStructure { Sections = ParseMajorSections(rulesText) };

      return await Task.FromResult(structure);
    };
  }

  /// <summary>
  /// Parses major sections (e.g., "1. Game Concepts").
  /// </summary>
  private static List<MajorSection> ParseMajorSections(string text)
  {
    var sections = new List<MajorSection>();
    var matches = MajorSectionRegex().Matches(text);

    for (int i = 0; i < matches.Count; i++)
    {
      var match = matches[i];
      var sectionNumber = int.Parse(match.Groups[1].Value);
      var sectionTitle = TextNormalizer.NormalizeText(match.Groups[2].Value.Trim());

      // Extract content between this section and the next
      var startIndex = match.Index + match.Length;
      var endIndex = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;
      var sectionContent = text[startIndex..endIndex];

      sections.Add(
        new MajorSection
        {
          Number = sectionNumber,
          Title = sectionTitle,
          Subsections = ParseSubsections(sectionContent),
        }
      );
    }

    return sections;
  }

  /// <summary>
  /// Parses subsections (e.g., "100. General").
  /// </summary>
  private static List<Subsection> ParseSubsections(string text)
  {
    var subsections = new List<Subsection>();
    var matches = SubsectionRegex().Matches(text);

    for (int i = 0; i < matches.Count; i++)
    {
      var match = matches[i];
      var subsectionNumber = int.Parse(match.Groups[1].Value);
      var subsectionTitle = TextNormalizer.NormalizeText(match.Groups[2].Value.Trim());

      // Extract content between this subsection and the next
      var startIndex = match.Index + match.Length;
      var endIndex = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;
      var subsectionContent = text[startIndex..endIndex];

      subsections.Add(
        new Subsection
        {
          Number = subsectionNumber,
          Title = subsectionTitle,
          Rules = ParseRules(subsectionContent),
        }
      );
    }

    return subsections;
  }

  /// <summary>
  /// Parses individual rules and their subrules.
  /// </summary>
  private static List<Rule> ParseRules(string text)
  {
    var rules = new List<Rule>();
    var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    Rule? currentRule = null;

    foreach (var line in lines)
    {
      var trimmed = line.Trim();
      if (string.IsNullOrEmpty(trimmed))
      {
        continue;
      }

      // Match main rule (e.g., "100.1. Text..." or "100.1a. Text...")
      var ruleMatch = RuleRegex().Match(trimmed);
      if (ruleMatch.Success)
      {
        var ruleNumber = ruleMatch.Groups[1].Value;
        var ruleText = TextNormalizer.NormalizeText(ruleMatch.Groups[2].Value.Trim());

        // Check if this is a subrule (has letter suffix)
        if (HasLetterSuffix(ruleNumber))
        {
          // This is a subrule
          var letter = GetLetterSuffix(ruleNumber);
          if (currentRule != null)
          {
            currentRule.Subrules.Add(new Subrule { Letter = letter, Text = ruleText });
          }
        }
        else
        {
          // Save previous rule
          if (currentRule != null)
          {
            rules.Add(currentRule);
          }

          // Start new rule
          currentRule = new Rule
          {
            Number = ruleNumber,
            Text = ruleText,
            Subrules = new List<Subrule>(),
          };
        }
      }
      else if (currentRule != null)
      {
        // Continuation of current rule text (no rule number prefix)
        currentRule = currentRule with
        {
          Text = currentRule.Text + " " + TextNormalizer.NormalizeText(trimmed),
        };
      }
    }

    // Add last rule
    if (currentRule != null)
    {
      rules.Add(currentRule);
    }

    return rules;
  }

  private static bool HasLetterSuffix(string ruleNumber)
  {
    var parts = ruleNumber.Split('.');
    if (parts.Length < 2)
    {
      return false;
    }

    var lastPart = parts[^1];
    return lastPart.Any(char.IsLetter);
  }

  private static string GetLetterSuffix(string ruleNumber)
  {
    var parts = ruleNumber.Split('.');
    var lastPart = parts[^1];
    return new string(lastPart.Where(char.IsLetter).ToArray());
  }

  // Regex patterns using C# 11+ source generators
  [GeneratedRegex(@"(?:\n|^)(\d)\.\s+(.+?)\n", RegexOptions.Multiline)]
  private static partial Regex MajorSectionRegex();

  [GeneratedRegex(@"(?:\n|^)(\d{3})\.\s+(.+?)\n", RegexOptions.Multiline)]
  private static partial Regex SubsectionRegex();

  // Matches both main rules (103.4.) and subrules (103.4a) - period after number is optional
  [GeneratedRegex(@"^(\d{3}\.\d+[a-z]*)\.?\s+(.+)$", RegexOptions.None)]
  private static partial Regex RuleRegex();
}
