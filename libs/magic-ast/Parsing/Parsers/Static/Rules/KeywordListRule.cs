namespace MagicAST.Parsing.Parsers.Static;

using MagicAST.AST.Abilities;
using MagicAST.Parsing;
using MagicAST.Parsing.Combinators;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;

[StaticRule(Priority = 1000)]
public sealed class KeywordListRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var tokens = clause.Tokens;
    // Try to parse using the OracleParsers.KeywordList combinator
    var parseResult = OracleParsers.KeywordList(tokens);

    if (!parseResult.HasValue)
    {
      return null;
    }

    // Convert StaticAbility[] to IReadOnlyList<Ability>
    return parseResult.Value.Cast<Ability>().ToList();
  }
}
