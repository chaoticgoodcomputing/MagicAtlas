namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 942)]
public sealed class SpellsTargetingCostMoreRule : IStaticRule
{
  // "Spells your opponents cast that target this creature cost {N} more to cast."
  // Also matches the name form: "... that target [Name] cost {N} more to cast."
  // The non-capturing middle group covers both "this creature/permanent/artifact/
  // enchantment/land/planeswalker" and any other noun phrase (card name).
  // Amount is a single generic-mana digit inside braces: {1}, {2}, {3}, {4}.
  private static readonly Regex _spellsTargetingCostMorePattern = new(
    @"^\s*Spells\s+your\s+opponents\s+cast\s+that\s+target\s+(?:this\s+\w+|[A-Z][A-Za-z\s,']+?)\s+cost\s+\{(?<amount>\d+)\}\s+more\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _spellsTargetingCostMorePattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostIncreaseEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
          TargetedObject = ObjectReference.Self(),
          CasterFilter = ControllerFilter.Opponent,
        }],
      },
    ];
  }
}
