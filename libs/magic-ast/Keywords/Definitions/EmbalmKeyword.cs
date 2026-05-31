namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Embalm {cost}: [Cost], Exile this card from your graveyard: Create a token that's a
/// copy of it, except it's a white Zombie [subtype(s)] with no mana cost. Embalm only
/// as a sorcery. Rule 702.88. An activated ability playable only from the graveyard.
/// MAST records the keyword and its associated mana cost; the token-creation structure
/// is inferred from the rules. Combinator-only: no matching <c>KeywordDefinitions</c>
/// entry exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class EmbalmKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Embalm")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Embalm",
      Effects = [new EmbalmEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
