namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 957)]
public sealed class EnchantedCantBeBlockedByMoreThanOneRule : IStaticRule
{
  private static readonly Regex _enchantedCantBeBlockedByMoreThanOnePattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+can'?t\s+be\s+blocked\s+by\s+more\s+than\s+one\s+creature\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_enchantedCantBeBlockedByMoreThanOnePattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Combat.CantBeBlockedEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            MaxBlockers = 1,
          },
        ],
      },
    ];
  }
}
