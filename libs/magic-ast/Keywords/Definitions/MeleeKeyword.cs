namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Melee: Whenever this creature attacks, it gets +1/+1 until end of turn for each
/// opponent you attacked with a creature this combat.
/// Rule 702.121. Although mechanically a triggered ability, MAST models it as a
/// keyword marker (same approach as Flanking, Evolve, Exalted); the attack-count
/// comparison and +1/+1 grant are engine territory.
/// </summary>
[Keyword]
public sealed class MeleeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Melee",
      RuleReference = "702.121",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Melee",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Melee }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Melee")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Melee",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Melee }],
      Reminder = reminder,
    }
  );
}
