namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Soulbond: You may pair this creature with another unpaired creature when either
/// enters. They remain paired for as long as you control both of them.
/// Rule 702.95. MAST records the keyword's presence; the pairing mechanics and any
/// granted abilities are engine territory (same approach as Flanking and Evolve).
/// </summary>
[Keyword]
public sealed class SoulbondKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Soulbond",
      RuleReference = "702.95",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Soulbond",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Soulbond }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Soulbond")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Soulbond",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Soulbond }],
      Reminder = reminder,
    }
  );
}
