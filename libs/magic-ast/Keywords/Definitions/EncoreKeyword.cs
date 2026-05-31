namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Encore {cost}: [Cost], Exile this card from your graveyard: For each opponent,
/// create a token copy of this card that attacks that opponent this turn if able.
/// Those tokens gain haste. Sacrifice those tokens at the beginning of the next end step.
/// Rule 702.142. An activated ability playable only from the graveyard. MAST records
/// the keyword and its associated mana cost; the token-copy-per-opponent structure is
/// inferred from the rules. Combinator-only: no matching <c>KeywordDefinitions</c>
/// entry exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class EncoreKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Encore")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Encore",
      Effects = [new EncoreEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
