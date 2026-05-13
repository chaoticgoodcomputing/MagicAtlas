using System.Text.RegularExpressions;
using MagicAtlas.Data._03_Primary.Schemas;
using Flowthru.Step;

namespace MagicAtlas.Flows.OracleEmbedding;

/// <summary>
/// Splits each card's oracle text into per-ability fragments and classifies them by type
/// (keyword / named_triggered / triggered / activated / passive). Each surviving fragment
/// becomes an <see cref="OracleInput"/> row — cards with multiple abilities produce multiple rows.
/// </summary>
[FlowthruStep]
public static partial class ProjectOracleInputNode
{
  [GeneratedRegex(@"\s*\([^)]*\)", RegexOptions.Compiled)]
  private static partial Regex ParentheticalPattern();

  // "Name — When/Whenever/At the beginning..." — em-dash OR en-dash OR hyphen-minus.
  [GeneratedRegex(@"^[A-Z][A-Za-z0-9\s,'’]*?\s[—–-]\s*(When|Whenever|At the beginning)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
  private static partial Regex NamedTriggeredPattern();

  [GeneratedRegex(@"^(When|Whenever|At the beginning)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
  private static partial Regex TriggeredPattern();

  // Activated: "<cost>: <effect>". Cost is a short prefix before the first colon, typically
  // containing mana symbols, tap symbols, or comma-separated sub-costs.
  [GeneratedRegex(@"^[^.:]{1,60}:\s", RegexOptions.Compiled)]
  private static partial Regex ActivatedPattern();

  public static Func<IEnumerable<CardCoreData>, Task<IEnumerable<OracleInput>>> Create() =>
    cards => Task.FromResult<IEnumerable<OracleInput>>(
      cards.SelectMany(ExtractFragments).ToList()
    );

  private static IEnumerable<OracleInput> ExtractFragments(CardCoreData card)
  {
    var text = card.OracleText;
    if (string.IsNullOrWhiteSpace(text)) yield break;

    // Strip reminder text in parentheses — it's explanatory, not semantic.
    text = ParentheticalPattern().Replace(text, "");

    foreach (var rawLine in text.Split('\n'))
    {
      var line = rawLine.Trim();
      if (string.IsNullOrWhiteSpace(line)) continue;

      yield return new OracleInput
      {
        PointId = Guid.NewGuid(),
        CardId = card.Id,
        Text = line,
        TextType = Classify(line),
      };
    }
  }

  private static string Classify(string line)
  {
    if (NamedTriggeredPattern().IsMatch(line)) return "named_triggered";
    if (TriggeredPattern().IsMatch(line)) return "triggered";
    if (ActivatedPattern().IsMatch(line)) return "activated";
    if (IsKeywordLine(line)) return "keyword";
    return "passive";
  }

  /// <summary>
  /// Keyword-ability heuristic: short line (≤60 chars), mostly letters and commas,
  /// no sentence structure (no mid-string periods). Catches "Flying", "Haste, Trample",
  /// "Double strike", "Ward {2}" while rejecting sentence-form rules text.
  /// </summary>
  private static bool IsKeywordLine(string line)
  {
    if (line.Length > 60) return false;
    var body = line.TrimEnd('.');
    if (body.Contains('.')) return false; // no mid-string sentences
    return body.Split(',').All(w => w.Trim().Length is > 0 and < 40);
  }
}
