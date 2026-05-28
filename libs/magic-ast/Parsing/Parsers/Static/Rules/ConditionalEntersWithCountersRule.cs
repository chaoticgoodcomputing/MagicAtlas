namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 951)]
public sealed class ConditionalEntersWithCountersRule : IStaticRule
{
  // "This [subject] enters with N <counterType> counters on it if [condition]."
  // Subject is any non-empty leading word sequence before "enters with".
  // Count may be a decimal digit, "X", an English word-count ("one"–"ten"),
  // or the article "a"/"an" (treated as 1 via TryParseSmallCount).
  // The "if" suffix is non-optional; the condition phrase is captured verbatim
  // and stored on Condition.Text. An optional trailing period is consumed.
  // Rules: 122 (counters), 614.1d (enters-with replacement).
  private static readonly Regex _conditionalEntersWithCountersPattern = new(
    @"^\s*\S.+?\s+enters\s+with\s+(?<count>\d+|X|an?|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\s+if\s+(?<condition>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _conditionalEntersWithCountersPattern.Match(clause.RawText);
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
    var conditionText = match.Groups["condition"].Value.Trim();

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Counter.PutCountersEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          Count = count,
          CounterType = counterType,
          IsOptional = false,
        }],
        Condition = new MagicAST.AST.Abilities.Condition
        {
          Text = conditionText,
        },
      },
    ];
  }
}
