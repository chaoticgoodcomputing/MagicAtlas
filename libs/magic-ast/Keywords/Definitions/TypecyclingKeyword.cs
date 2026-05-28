namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// [Type]cycling [cost]: Discard this card: Search your library for a [Type] card,
/// reveal it, put it into your hand, then shuffle.
/// Rule 702.28. Catch-all parser for land-type cycling variants (Swampcycling,
/// Forestcycling, etc.). The land type is extracted from the keyword prefix.
/// MAST records the keyword, the land type, and the cost; the inner
/// search/reveal/shuffle structure is inferred from the rules.
/// Lower priority (40) so specific cycling variants can outrank this catch-all
/// in the registry Or-chain.
/// </summary>
[Keyword(Priority = 40)]
public sealed class TypecyclingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kwToken in Token
      .EqualTo(OracleToken.Word)
      .Try()
      .Where(t =>
      {
        var s = t.ToStringValue();
        return s.EndsWith("cycling", StringComparison.OrdinalIgnoreCase)
          && !s.Equals("cycling", StringComparison.OrdinalIgnoreCase);
      })
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    let kwText = kwToken.ToStringValue()
    let landType = kwText[..^"cycling".Length]
    select new StaticAbility
    {
      KeywordSource = kwText,
      Effects = [new TypecyclingEffect
      {
        Type = char.ToUpperInvariant(landType[0]) + landType[1..],
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
