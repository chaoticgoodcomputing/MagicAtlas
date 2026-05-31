namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Freerunning {cost}: You may cast this spell for its freerunning cost if you
/// dealt combat damage to a player this turn with an Assassin or commander.
/// Rule 702.166. MAST records the keyword and its alternative mana cost; the
/// combat-damage condition and the alternative-cast semantics are inferred from
/// the rules. Combinator-only: no <see cref="KeywordDefinition"/> exists in the
/// legacy registry. <see cref="Definition"/> returns <c>null</c>.
/// </summary>
[Keyword]
public sealed class FreerunningKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Freerunning")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Freerunning",
      Effects = [new FreerunningEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
