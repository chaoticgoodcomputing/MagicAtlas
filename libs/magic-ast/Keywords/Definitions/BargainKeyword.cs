namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Bargain: You may sacrifice an artifact, enchantment, or token as you cast
/// this spell.
/// Rule 702.166. MAST records the keyword's presence; the optional-sacrifice
/// additional-cost and "bargained" designation gating conditional effects are
/// engine territory.
/// </summary>
[Keyword]
public sealed class BargainKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Bargain",
      RuleReference = "702.166",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Bargain",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Bargain }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Bargain")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Bargain",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Bargain }],
      Reminder = reminder,
    }
  );
}
