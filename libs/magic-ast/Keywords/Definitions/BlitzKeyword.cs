namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Blitz {cost}: An alternative casting cost. If you cast this spell for its
/// blitz cost, it gains haste and "When this creature dies, draw a card.", and
/// you sacrifice it at the beginning of the next end step.
/// Rule 702.152. MAST records the keyword and its associated mana cost; the
/// granted haste, death-draw trigger, and sacrifice are inferred from the rules.
/// Combinator-only: no matching <c>KeywordDefinitions</c> entry exists in the
/// legacy registry.
/// </summary>
[Keyword]
public sealed class BlitzKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Blitz")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Blitz",
      Effects = [new BlitzEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
