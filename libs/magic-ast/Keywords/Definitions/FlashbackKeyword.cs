namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Flashback {cost}: You may cast this card from your graveyard for its flashback cost.
/// Then exile it. Rule 702.34. MAST records the keyword and the flashback cost;
/// cast-from-graveyard-then-exile machinery is engine territory.
/// Combinator-only keyword — no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions</c> registry.
/// </summary>
[Keyword]
public sealed class FlashbackKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Flashback")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flashback",
      Effects = [new FlashbackEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
