namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 991)]
public sealed class GainControlOfEnchantedRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_gainControlEnchantedPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new GainControlEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
        }],
      },
    ];
  }

  private static readonly Regex _gainControlEnchantedPattern = new(
    @"^\s*You\s+control\s+enchanted\s+(?:creature|permanent|land|artifact|enchantment|planeswalker|equipment)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
