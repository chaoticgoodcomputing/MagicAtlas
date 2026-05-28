namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 966)]
public sealed class EnchantedPTAndDualKeywordRule : IStaticRule
{
  // "(Enchanted|Equipped) creature gets +N/+M and has <kw1> and <kw2>."
  // kw1 is a single keyword (no internal spaces containing " and "); kw2 is
  // captured to the end-anchor. Two-word keywords (e.g. "first strike") are
  // handled because neither capture group contains the literal token " and "
  // as a separator — the separator is the explicit \s+and\s+ between the two
  // named groups. The non-greedy kw1 stops at the first " and " it sees, so
  // "flying and first strike" routes kw1="flying", kw2="first strike" correctly.
  private static readonly Regex _enchantedPTAndDualKeywordPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+(?<kw1>[a-z][a-z]*(?:\s+[a-z]+)*?)\s+and\s+(?<kw2>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _enchantedPTAndDualKeywordPattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    var kw1 = match.Groups["kw1"].Value.Trim().ToLowerInvariant();
    var kw2 = match.Groups["kw2"].Value.Trim().ToLowerInvariant();

    var grantedAbility1 = StaticRuleHelpers.MapKeywordToStaticAbility(kw1);
    var grantedAbility2 = StaticRuleHelpers.MapKeywordToStaticAbility(kw2);

    if (grantedAbility1 is null || grantedAbility2 is null)
    {
      // One or both keywords unrecognised — fall through to the fallback.
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Core.CompositeEffect
        {
          Effects =
          [
            new ModifyPTEffect
            {
              Target = target,
              PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
              ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            },
            new GainAbilityEffect
            {
              Target = target,
              GainedAbility = grantedAbility1,
            },
            new GainAbilityEffect
            {
              Target = target,
              GainedAbility = grantedAbility2,
            },
          ],
        }],
      },
    ];
  }
}
