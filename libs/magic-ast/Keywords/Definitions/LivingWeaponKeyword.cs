namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Living weapon: When this Equipment enters, create a 0/0 black Phyrexian
/// Germ creature token, then attach this to it.
/// Rule 702.77. Although mechanically a triggered ability, MAST records it as
/// a keyword marker — same approach as Evolve and Flanking; the ETB trigger,
/// token-creation, and auto-attach semantics are engine territory.
/// Multi-word keyword matched by chaining two Keyword() calls ("Living" then "weapon").
/// </summary>
[Keyword]
public sealed class LivingWeaponKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Living weapon",
      RuleReference = "702.77",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Living weapon",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.LivingWeapon }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from living in Keyword("Living")
    from weapon in Keyword("weapon")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Living weapon",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.LivingWeapon }],
      Reminder = reminder,
    }
  );
}
