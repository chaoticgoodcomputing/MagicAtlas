namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Delve: Each card you exile from your graveyard while casting this spell pays for {1}.
/// Rule 702.66. A parameterless cost-modifier keyword — MAST records the keyword's
/// presence; the per-card graveyard-exile cost-reduction mechanic is engine territory.
/// </summary>
[Keyword]
public sealed class DelveKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Delve",
      RuleReference = "702.66",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Delve",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Delve }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Delve")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Delve",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Delve }],
      Reminder = reminder,
    }
  );
}
