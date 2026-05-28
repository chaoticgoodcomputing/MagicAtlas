namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Sneak {cost}: An alternative-cost keyword found on spells. The caster
/// may pay the Sneak cost instead of the regular mana cost if they also
/// return an unblocked attacker they control to hand during the declare
/// blockers step (Rule 702.173). MAST records the keyword and its mana
/// cost; the attacker-return condition is reminder text inferred from rules.
/// </summary>
[Keyword]
public sealed class SneakKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Sneak")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Sneak",
      Effects = [new SneakEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
