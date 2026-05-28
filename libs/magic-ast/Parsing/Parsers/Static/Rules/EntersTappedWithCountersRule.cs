namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

[StaticRule(Priority = 964)]
public sealed class EntersTappedWithCountersRule : IStaticRule
{
  private static readonly Regex _entersTappedWithCountersPattern = new(
    @"^\s*This\s+land\s+enters\s+tapped\s+with\s+(?<count>\d+|X|an?|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _entersTappedWithCountersPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var countText = match.Groups["count"].Value;
    MagicAST.AST.Quantities.Quantity count;
    if (countText.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      count = MagicAST.AST.Quantities.VariableQuantity.X;
    }
    else if (StaticRuleHelpers.TryParseSmallCount(countText.ToLowerInvariant(), out var intCount))
    {
      count = MagicAST.AST.Quantities.LiteralQuantity.Of(intCount);
    }
    else
    {
      return null;
    }

    var counterType = match.Groups["counterType"].Value;

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Keyword.EntersTappedEffect(),
          new MagicAST.AST.Effects.Replacement.EntersWithCountersEffect
          {
            Count = count,
            CounterType = counterType,
            IsOptional = false,
          },
        ],
      },
    ];
  }
}
