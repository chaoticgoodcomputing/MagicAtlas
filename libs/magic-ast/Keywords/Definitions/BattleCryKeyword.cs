namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Battle cry: Whenever this creature attacks, each other attacking creature gets
/// +1/+0 until end of turn.
/// Rule 702.91. Although mechanically a triggered ability, MAST models it as a
/// keyword marker (same approach as Flanking, Evolve, Exalted); the attack trigger
/// and pump expansion are engine territory.
/// </summary>
[Keyword]
public sealed class BattleCryKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Battle cry",
      RuleReference = "702.91",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Battle cry",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.BattleCry }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from battle in Keyword("Battle")
    from cry in Keyword("cry")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Battle cry",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.BattleCry }],
      Reminder = reminder,
    }
  );
}
