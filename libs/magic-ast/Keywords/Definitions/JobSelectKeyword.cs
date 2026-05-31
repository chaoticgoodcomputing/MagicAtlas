namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Job select: When this Equipment enters, create a 1/1 colorless Hero creature token,
/// then attach this to it.
/// Rule 702.182. Found on Equipment cards from the Final Fantasy set. Although
/// mechanically a triggered ability, MAST records it as a keyword marker — same
/// approach as Living weapon (702.77); the ETB trigger, Hero-token creation, and
/// auto-attach semantics are engine territory.
/// Multi-word keyword via sequential Keyword() combinators, mirroring LivingWeapon
/// and BattleCry.
/// </summary>
[Keyword]
public sealed class JobSelectKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Job select",
      RuleReference = "702.182",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.JobSelect,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.JobSelect }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from job in Keyword("Job")
    from selectKw in Keyword("select")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.JobSelect,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.JobSelect }],
      Reminder = reminder,
    }
  );
}
