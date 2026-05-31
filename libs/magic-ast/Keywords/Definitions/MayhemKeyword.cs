namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Mayhem {cost}: An alternative-cost keyword allowing a card to be cast from
/// the graveyard for its mayhem cost if it was discarded this turn.
/// Rule 702.187. MAST records the keyword and the alternative cost; the
/// discard-condition and graveyard-cast mechanics are reminder text and
/// engine territory.
///
/// <para>
/// Combinator-only keyword: Mayhem has no entry in <c>KeywordDefinitions.All</c>
/// (it is not registered as a <see cref="KeywordDefinition"/>), so
/// <see cref="Definition"/> is <see langword="null"/>.
/// </para>
/// </summary>
[Keyword]
public sealed class MayhemKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Mayhem")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Mayhem,
      Effects = [new MayhemEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
