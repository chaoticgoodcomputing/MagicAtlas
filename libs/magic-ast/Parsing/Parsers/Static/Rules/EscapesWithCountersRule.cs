namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 952)]
public sealed class EscapesWithCountersRule : IStaticRule
{
  // Matches "This <type> escapes with N <counterType> counters on it." OR
  // "[CardName] escapes with N <counterType> counters on it." — both "This
  // [type]" and a named self-reference are valid oracle forms for the same
  // conditional-entry replacement. The subject prefix is captured liberally as
  // "any non-empty leading words before 'escapes with'", consistent with how
  // EntersWithCountersRule treats the sibling "enters with" template. N is a
  // decimal digit, the variable "X", an English word-count ("one" through
  // "ten"), or the article "a"/"an" (treated as 1). The counter type may be a
  // P/T counter ("+1/+1", "-1/-1") or any named counter. Handles "counter" and
  // "counters" and an optional trailing period.
  //
  // CR 702.138c (verbatim): "An ability that reads "[This permanent] escapes
  // with [one or more of a kind of counter]" means "If this permanent escaped,
  // it enters with [those counters]" ..." — reduced here to the same
  // AsThisEnters + PutCountersEffect shape as EntersWithCountersRule, gated by
  // an EscapedCondition (CR 702.138b defines "escaped").
  private static readonly Regex _escapesWithCountersPattern = new(
    @"^\s*\S.+?\s+escapes\s+with\s+(?<count>\d+|X|an?|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _escapesWithCountersPattern.Match(StaticRuleHelpers.StripReminderText(clause.RawText));
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
        Condition = new EscapedCondition(),
      },
    ];
  }
}
