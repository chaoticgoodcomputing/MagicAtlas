namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// For Mirrodin! (Phyrexia: All Will Be One). When this Equipment enters,
/// create a 2/2 red Rebel creature token, then attach this to it. Although
/// mechanically a triggered ability, MAST records it as a keyword marker —
/// same approach as Living Weapon (Rule 702.77); the ETB trigger,
/// token-creation, and auto-attach semantics are engine territory.
/// The '!' in oracle text is silently dropped by the tokenizer.
/// </summary>
[Keyword]
public sealed class ForMirrodinKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "For Mirrodin",
      RuleReference = "702.77",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "For Mirrodin",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.ForMirrodin }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from for_ in Keyword("For")
    from mirrodin in Keyword("Mirrodin")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "For Mirrodin",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.ForMirrodin }],
      Reminder = reminder,
    }
  );
}
