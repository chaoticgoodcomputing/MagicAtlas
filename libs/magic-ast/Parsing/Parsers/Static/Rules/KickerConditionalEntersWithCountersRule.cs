namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 952)]
public sealed class KickerConditionalEntersWithCountersRule : IStaticRule
{
  // "If this creature was kicked, it enters with N <counterType> counters on it."
  // The condition phrase is fixed ("this creature was kicked"); count may be a
  // decimal digit, an English word-count ("one" through "ten"), or the article
  // "a"/"an" (treated as 1). Counter type is any single alphanumeric/symbol token
  // immediately before "counter(s)" — covers "+1/+1" and named counters.
  private static readonly Regex _kickerConditionalEntersWithCountersPattern = new(
    @"^\s*If\s+this\s+creature\s+was\s+kicked,\s+it\s+enters\s+with\s+(?<count>\d+|an?|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _kickerConditionalEntersWithCountersPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var countText = match.Groups["count"].Value;
    MagicAST.AST.Quantities.Quantity count;
    if (StaticRuleHelpers.TryParseSmallCount(countText.ToLowerInvariant(), out var intCount))
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
          Text = "this creature was kicked",
        },
      },
    ];
  }
}
