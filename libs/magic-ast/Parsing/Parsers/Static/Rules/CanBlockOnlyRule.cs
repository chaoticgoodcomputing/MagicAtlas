namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 948)]
public sealed class CanBlockOnlyRule : IStaticRule
{
  // Matches "This creature can block only <filter>."
  // The filter group captures everything between "only " and the terminal period.
  private static readonly Regex _canBlockOnlyPattern = new(
    @"^\s*This\s+creature\s+can\s+block\s+only\s+(?<filter>.+?)\.\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches "creatures with <characteristic>" — the standard filter shape.
  private static readonly Regex _creaturesWithPattern = new(
    @"^creatures\s+with\s+(?<char>.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _canBlockOnlyPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterPhrase = match.Groups["filter"].Value.Trim();

    // "creatures with <X>" — the standard filter shape for this family.
    var withMatch = _creaturesWithPattern.Match(filterPhrase);
    if (!withMatch.Success)
    {
      return null;
    }

    var characteristic = withMatch.Groups["char"].Value.Trim().ToLowerInvariant();

    return
    [
      new StaticAbility
      {
        Effects = [new CanBlockOnlyEffect
        {
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [$"with {characteristic}"],
          },
        }],
      },
    ];
  }
}
