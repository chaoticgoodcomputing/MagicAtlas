namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Split second: As long as this spell is on the stack, players can't cast spells
/// or activate abilities that aren't mana abilities.
/// Rule 702.61. MAST records the keyword's presence; the stack-restriction
/// semantics are engine territory. Multi-word keyword via sequential Keyword()
/// combinators, mirroring FirstStrike and CumulativeUpkeep.
/// </summary>
[Keyword]
public sealed class SplitSecondKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Split second",
      RuleReference = "702.61",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Split second",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.SplitSecond }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from split in Keyword("Split")
    from second in Keyword("second")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Split second",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.SplitSecond }],
      Reminder = reminder,
    }
  );
}
