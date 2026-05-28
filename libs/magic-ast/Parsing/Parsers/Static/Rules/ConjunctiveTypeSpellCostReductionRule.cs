namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 983)]
public sealed class ConjunctiveTypeSpellCostReductionRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Phrasing A: "TypeA and TypeB spells you cast cost {N} less to cast."
    var matchA = _conjunctiveTypeSpellCostReductionPatternA.Match(clause.RawText);
    // Phrasing B: "TypeA spells and TypeB spells you cast cost {N} less to cast."
    var matchB = _conjunctiveTypeSpellCostReductionPatternB.Match(clause.RawText);

    string typeA, typeB;
    int amount;

    if (matchA.Success)
    {
      typeA  = matchA.Groups["typeA"].Value.Trim();
      typeB  = matchA.Groups["typeB"].Value.Trim();
      amount = int.Parse(matchA.Groups["amount"].Value);
    }
    else if (matchB.Success)
    {
      typeA  = matchB.Groups["typeA"].Value.Trim();
      typeB  = matchB.Groups["typeB"].Value.Trim();
      amount = int.Parse(matchB.Groups["amount"].Value);
    }
    else
    {
      return null;
    }

    var affected = StaticRuleHelpers.BuildConjunctiveTypeSpellFilter(typeA, typeB);
    if (affected is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        }],
        AffectedObjects = affected,
      },
    ];
  }

  // Phrasing A: "Artifact and enchantment spells you cast cost {1} less to cast."
  // typeA is capitalised; typeB is lowercase (oracle style: "Instant and sorcery").
  private static readonly Regex _conjunctiveTypeSpellCostReductionPatternA = new(
    @"^\s*(?<typeA>[A-Z][A-Za-z]+)\s+and\s+(?<typeB>[A-Za-z]+)\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  // Phrasing B: "Kithkin spells and Soldier spells you cast cost {1} less to cast."
  private static readonly Regex _conjunctiveTypeSpellCostReductionPatternB = new(
    @"^\s*(?<typeA>[A-Z][A-Za-z]+)\s+spells\s+and\s+(?<typeB>[A-Za-z]+)\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );
}
