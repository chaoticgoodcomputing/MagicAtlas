namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Disguise {cost}: An alternative casting mode that puts the card face down as a
/// 2/2 creature with ward {2}. The controller may turn it face up at any time by
/// paying the disguise cost.
/// Rule 702.168. MAST records the keyword and its associated mana cost; the
/// face-down/ward and turn-face-up semantics are described in the Reminder
/// parenthetical.
/// </summary>
[Keyword]
public sealed class DisguiseKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Disguise")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Disguise,
      Effects = [new DisguiseEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
