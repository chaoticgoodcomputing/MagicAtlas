namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 986)]
public sealed class ConditionalSelfKeywordRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*(?<cond>During\s+(?:your\s+turn|each\s+(?:opponent|player)'?s\s+turn|combat)),\s+(?<subject>\S.*?)\s+has\s+(?<kw>\w+(?:\s+\w+)?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var kw = match.Groups["kw"].Value.Trim();
    var conditionText = match.Groups["cond"].Value.Trim();
    // Delegate to the shared keyword→StaticAbility map so that every keyword
    // supported by TryParseKeywordList is also supported here.
    var keywordAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (keywordAbility is null)
    {
      return null;
    }
    // Attach the "During [period]" condition to the mapped ability.
    return
    [
      keywordAbility with
      {
        Condition = MagicAST.Parsing.ConditionParser.Parse(conditionText),
      },
    ];
  }
}
