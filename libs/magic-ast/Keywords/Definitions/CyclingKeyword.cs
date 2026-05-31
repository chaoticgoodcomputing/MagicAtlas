namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Cycling {cost}: [Cost], Discard this card: Draw a card.
/// Rule 702.29. An activated ability playable only from hand. MAST records the keyword
/// and its associated mana cost; the inner discard/draw structure is inferred from the
/// rules. Combinator-only: no matching <c>KeywordDefinitions</c> entry exists in the
/// legacy registry.
/// </summary>
[Keyword]
public sealed class CyclingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Cycling")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Cycling",
      Effects = [new CyclingEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
