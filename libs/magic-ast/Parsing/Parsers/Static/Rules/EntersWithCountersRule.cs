namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 950)]
public sealed class EntersWithCountersRule : IStaticRule
{
  // Matches "This <type> enters with N <counterType> counters on it." OR
  // "[CardName] enters with N <counterType> counters on it." — both "This [type]"
  // and a named self-reference are valid oracle forms for the same replacement
  // (Rule 614.1d). The subject prefix is captured liberally as "any non-empty
  // leading words before 'enters with'", consistent with how MustAttack and
  // MustBeBlocked treat named self-references (collapsed to Self in the AST).
  // N is a decimal digit, the variable "X", an English word-count ("one"
  // through "ten"), or the article "a"/"an" (treated as 1). The counter type
  // may be a P/T counter ("+1/+1", "-1/-1") or any named counter.
  // Handles "counter" and "counters" and an optional trailing period.
  // Rules: 122 (counters), 614.1d (enters-with replacement).
  private static readonly Regex _entersWithCountersPattern = new(
    @"^\s*\S.+?\s+enters\s+with\s+(?<count>\d+|X|an?|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _entersWithCountersPattern.Match(StaticRuleHelpers.StripReminderText(clause.RawText));
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
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Counter.PutCountersEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          Count = count,
          CounterType = counterType,
        }],
      },
    ];
  }
}
