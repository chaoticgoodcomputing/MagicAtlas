namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 965)]
public sealed class EnchantedPTAndKeywordRule : IStaticRule
{
  private static readonly Regex _enchantedPTAndKeywordPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Strip trailing parenthetical reminder text before matching so lines like
    // "Enchanted creature gets +2/+0 and has trample. (It can deal excess...)"
    // still match the end-anchored pattern (Rule 207.2 — reminder text is not rules text).
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _enchantedPTAndKeywordPattern.Match(rawText);
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

    var kw = match.Groups["kw"].Value.Trim();
    var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (grantedAbility is null)
    {
      // Unrecognised keyword — fall through so the fallback surfaces the gap.
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
              GainedAbility = grantedAbility,
            },
          ],
        }],
      },
    ];
  }
}
