namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Doctor's companion (Doctor Who Commander). You can have two commanders
/// if the other is the Doctor. A variant of the Partner keyword restricted
/// to Doctor-subtype commanders. MAST records the keyword's presence;
/// the commander-pairing restriction is engine territory.
/// Mirrors Partner (Rule 702.124) but with the Doctor-constraint.
/// </summary>
[Keyword]
public sealed class DoctorsCompanionKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Doctor's companion",
      RuleReference = "702.124",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.DoctorsCompanion,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.DoctorsCompanion }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from doctors in Keyword("Doctor's")
    from companion in Keyword("companion")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.DoctorsCompanion,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.DoctorsCompanion }],
      Reminder = reminder,
    }
  );
}
