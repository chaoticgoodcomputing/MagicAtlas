namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Entwine [cost]: Choose both if you pay the entwine cost.
/// Rule 702.42. The cost is parsed by the shared mana-cost parser so
/// multi-symbol entwine costs land as full ManaCost nodes rather than
/// free-text fragments.
/// Combinator-only: no KeywordDefinition entry in the legacy registry.
/// </summary>
[Keyword]
public sealed class EntwineKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Entwine")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Entwine",
      Effects = [new EntwineEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
