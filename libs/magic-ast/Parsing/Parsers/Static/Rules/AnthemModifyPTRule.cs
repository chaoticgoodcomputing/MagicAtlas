namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 972)]
public sealed class AnthemModifyPTRule : IStaticRule
{
  private static readonly Regex _anthemModifyPTPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _anthemModifyPTPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = int.Parse(match.Groups["psign"].Value + match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["tsign"].Value + match.Groups["t"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }
}
