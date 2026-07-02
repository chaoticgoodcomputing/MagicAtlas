namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Horsemanship: This creature can't be blocked except by creatures with horsemanship.
/// Rule 702.32. An evasion keyword — MAST records the keyword's presence; the
/// mutual-horsemanship blocking restriction is engine territory.
/// </summary>
[Keyword]
public sealed class HorsemanshipKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Horsemanship",
      RuleReference = "702.32",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.Horsemanship,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Horsemanship }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Horsemanship")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Horsemanship,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Horsemanship }],
      Reminder = reminder,
    }
  );
}
