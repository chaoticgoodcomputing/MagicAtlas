namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Flanking: Whenever a creature without flanking blocks this creature, the blocking
/// creature gets -1/-1 until end of turn.
/// Rule 702.25. Although mechanically triggered, MAST models it as a keyword marker
/// (same approach as Exalted); the trigger-and-debuff expansion is engine territory.
/// </summary>
[Keyword]
public sealed class FlankingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Flanking",
      RuleReference = "702.25",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Flanking",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Flanking }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Flanking")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flanking",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Flanking }],
      Reminder = reminder,
    }
  );
}
