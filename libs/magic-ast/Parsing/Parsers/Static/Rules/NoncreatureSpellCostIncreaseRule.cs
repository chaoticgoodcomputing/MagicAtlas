namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 982)]
public sealed class NoncreatureSpellCostIncreaseRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _noncreatureSpellCostIncreasePattern.Match(clause.RawText);
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
        }],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          ExcludedCardTypes = ["creature"],
        },
      },
    ];
  }

  // "Noncreature spells cost {N} more to cast."
  // No "you cast" suffix — the tax applies to all players' noncreature spells.
  // Anchored at both ends; optional trailing period.
  private static readonly Regex _noncreatureSpellCostIncreasePattern = new(
    @"^\s*Noncreature\s+spells\s+cost\s+\{(?<amount>\d+)\}\s+more\s+to\s+cast\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
