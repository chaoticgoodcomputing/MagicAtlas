namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Emerge {cost}: You may cast this spell by sacrificing a creature and paying
/// the emerge cost reduced by that creature's mana value.
/// Rule 702.119. MAST records the keyword and its associated mana cost; the
/// sacrifice mechanic, cost-reduction, and timing are inferred from the rules.
/// Combinator-only: no matching <c>KeywordDefinitions</c> entry exists in the
/// legacy registry.
/// </summary>
[Keyword]
public sealed class EmergeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Emerge")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Emerge",
      Effects = [new EmergeEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
