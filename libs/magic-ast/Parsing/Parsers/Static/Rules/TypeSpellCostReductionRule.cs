namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 984)]
public sealed class TypeSpellCostReductionRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _typeSpellCostReductionPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var amount = int.Parse(match.Groups["amount"].Value);

    var affected = StaticRuleHelpers.BuildTypeSpellFilter(filterText);
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

  // Pattern: a single capitalised noun (no internal spaces — keeps compound
  // filters like "Instant and sorcery" or "White creature" out of scope so
  // they fall through to the fallback for a future family), then
  // " spells you cast cost {N} less to cast." with optional trailing period.
  // Amount is restricted to a single generic-mana digit (the cluster covers
  // {1} and {2} cleanly); coloured-cost reductions are a separate family.
  private static readonly Regex _typeSpellCostReductionPattern = new(
    @"^\s*(?<filter>[A-Z][A-Za-z]+)\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );
}
