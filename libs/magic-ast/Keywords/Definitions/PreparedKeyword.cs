namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Prepared — keyword state printed as "This creature enters prepared." on
/// the front face of prepare-layout double-faced cards. While prepared, the
/// controller may cast a copy of the attached spell; doing so unprepares it.
/// MAST records the keyword's presence; the prepared-state and copy-cast
/// mechanics are engine territory per the descriptive-not-engine doctrine.
/// Rule 702.177.
/// </summary>
[Keyword]
public sealed class PreparedKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Prepared",
      RuleReference = "702.177",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Prepared",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Prepared }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from this_ in Token.EqualTo(OracleToken.This)
    from creature in Keyword("creature")
    from enters in Keyword("enters")
    from prepared in Keyword("prepared")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Prepared",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Prepared }],
      Reminder = reminder,
    }
  );
}
