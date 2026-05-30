namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 985)]
public sealed class ConditionalSpellCostReductionRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*This\s+spell\s+costs\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\s+(?<cond>"
        + @"during\s+(?:your\s+turn|each\s+(?:opponent|player)'?s\s+turn|combat)"
        + @"|if\s+you\s+control\s+an?\s+[A-Z][A-Za-z]+"
        + @"|if\s+it\s+targets\s+a\s+tapped\s+(?:creature|permanent)"
        + @")\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var amount = int.Parse(match.Groups["amount"].Value);
    // Preserve oracle-text casing for the condition verbatim so the fixture
    // comparison sees exactly what the oracle line says (e.g. "if you control
    // a Wizard", "during your turn").
    var conditionText = match.Groups["cond"].Value;
    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        }],
        Condition = MagicAST.Parsing.ConditionParser.Parse(conditionText),
      },
    ];
  }
}
