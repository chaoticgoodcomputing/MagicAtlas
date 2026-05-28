namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;

[StaticRule(Priority = 975)]
public sealed class DefinePTRule : IStaticRule
{
  // "[Name]'s power and toughness are each equal to the number of [filter]."
  // The <name> capture is non-greedy to stop at the possessive. The <filter>
  // capture grabs everything between "the number of " and the terminal period.
  private static readonly Regex _definePTBothPattern = new(
    @"^\s*.+?'s\s+power\s+and\s+toughness\s+are\s+each\s+equal\s+to\s+the\s+number\s+of\s+(?<filter>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "[Name]'s (power|toughness) is equal to the number of [filter]."
  private static readonly Regex _definePTSinglePattern = new(
    @"^\s*.+?'s\s+(?<which>power|toughness)\s+is\s+equal\s+to\s+the\s+number\s+of\s+(?<filter>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Try "power and toughness are each equal to" first (most specific).
    var bothMatch = _definePTBothPattern.Match(clause.RawText);
    if (bothMatch.Success)
    {
      var filter = bothMatch.Groups["filter"].Value.Trim();
      return
      [
        new StaticAbility
        {
          Effects = [new DefinePTEffect
          {
            Characteristic = PTCharacteristic.Both,
            Value = new MagicAST.AST.Quantities.CountQuantity { CountOf = filter },
          }],
        },
      ];
    }

    // Try "power is equal to" or "toughness is equal to".
    var singleMatch = _definePTSinglePattern.Match(clause.RawText);
    if (singleMatch.Success)
    {
      var which = singleMatch.Groups["which"].Value.ToLowerInvariant();
      var filter = singleMatch.Groups["filter"].Value.Trim();
      var characteristic = which == "power"
        ? PTCharacteristic.Power
        : PTCharacteristic.Toughness;

      return
      [
        new StaticAbility
        {
          Effects = [new DefinePTEffect
          {
            Characteristic = characteristic,
            Value = new MagicAST.AST.Quantities.CountQuantity { CountOf = filter },
          }],
        },
      ];
    }

    return null;
  }
}
