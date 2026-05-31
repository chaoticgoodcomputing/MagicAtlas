namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Eternalize {cost}: [Cost], Exile this card from your graveyard: Create a token
/// that's a copy of it, except it's a 4/4 black Zombie [subtype] with no mana cost.
/// Eternalize only as a sorcery.
/// Rule 702.91. An activated ability playable only from the graveyard. MAST records
/// the keyword and its associated mana cost; the token-copy, exile, and timing
/// restriction are inferred from the rules. Combinator-only: no matching
/// <c>KeywordDefinitions</c> entry exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class EternalizeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Eternalize")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Eternalize,
      Effects = [new EternalizeEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
