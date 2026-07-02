namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Transmute {cost}: {cost}, Discard this card: Search your library for a card
/// with the same mana value as this card, reveal it, put it into your hand, then
/// shuffle. Activate only as a sorcery.
/// Rule 702.49. MAST records the keyword and its associated mana cost; the inner
/// discard/search/reveal/shuffle structure is described by the rules and left to
/// the engine. Mirrors <see cref="CyclingKeyword"/> — combinator-only, no legacy
/// registry entry needed.
/// </summary>
[Keyword]
public sealed class TransmuteKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Transmute")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Transmute,
      Effects = [new TransmuteEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
