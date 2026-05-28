namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 953)]
public sealed class AbilityWordConditionalEntersWithCountersRule : IStaticRule
{
  // "[AbilityWord] — This creature enters with N <counterType> counters on it if <condition>."
  // The optional ability-word prefix (any word or two-word phrase before " — ") is captured
  // into <abilityWord>. The count token and counter type are as in _entersWithCountersPattern.
  // The condition is everything after "if " up to the terminal period.
  private static readonly Regex _abilityWordConditionalEntersWithCountersPattern = new(
    @"^\s*(?:(?<abilityWord>[A-Z][A-Za-z ]+?)\s+—\s+)?This\s+creature\s+enters\s+with\s+(?<count>\d+|an?|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\s+if\s+(?<condition>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _abilityWordConditionalEntersWithCountersPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var abilityWord = match.Groups["abilityWord"].Success
      ? match.Groups["abilityWord"].Value.Trim()
      : null;

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
    var conditionText = match.Groups["condition"].Value.Trim();

    return
    [
      new StaticAbility
      {
        AbilityWord = abilityWord,
        Effects = [new MagicAST.AST.Effects.Replacement.EntersWithCountersEffect
        {
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
