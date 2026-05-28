namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Recover [cost]: When a creature is put into your graveyard from the battlefield,
/// you may pay [cost]. If you do, return this card from your graveyard to your hand.
/// Otherwise, exile this card. Rule 702.59. A triggered ability functioning only
/// while the card is in a player's graveyard. MAST records the keyword and its
/// associated mana cost; the trigger, conditional return, and exile are inferred
/// from the rules. Combinator-only: no matching <c>KeywordDefinitions</c> entry
/// exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class RecoverKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Recover")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Recover",
      Effects = [new RecoverEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
