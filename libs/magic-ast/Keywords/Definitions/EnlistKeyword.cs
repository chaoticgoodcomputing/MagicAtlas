namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Enlist: As this creature attacks, you may tap an untapped creature you control
/// that could have attacked. If you do, add its power to this creature's until
/// end of turn.
/// Rule 702.154. Although mechanically a static + triggered ability pair, MAST
/// records it as a keyword marker — same approach as Evolve and Flanking.
/// The tapping cost and power-addition are engine territory.
/// </summary>
[Keyword]
public sealed class EnlistKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Enlist",
      RuleReference = "702.154",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.Enlist,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Enlist }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Enlist")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Enlist,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Enlist }],
      Reminder = reminder,
    }
  );
}
