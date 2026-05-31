namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Indestructible: This permanent can't be destroyed. Damage and "destroy" effects
/// that would destroy it have no effect. Rule 702.12. MAST records keyword presence;
/// engine-territory semantics are omitted.
/// </summary>
[Keyword]
public sealed class IndestructibleKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Indestructible",
      RuleReference = "702.12",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Indestructible",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Indestructible")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Indestructible",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible }],
      Reminder = reminder,
    }
  );
}
