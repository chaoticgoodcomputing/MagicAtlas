namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Improvise: Each artifact you tap after you're done activating mana abilities pays
/// for {1}. Rule 702.126. A parameterless cost-modifier keyword — MAST records the
/// keyword's presence; the per-artifact cost-reduction mechanic is engine territory.
/// </summary>
[Keyword]
public sealed class ImproviseKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Improvise",
      RuleReference = "702.126",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Improvise",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Improvise }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Improvise")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Improvise",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Improvise }],
      Reminder = reminder,
    }
  );
}
