namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Compleated (CR 702.150): a static ability on Phyrexian planeswalkers.
///
/// <para>
/// CR 702.150a (verbatim): "Compleated is a static ability found on some planeswalker
/// cards. Compleated means 'If this permanent would enter with one or more loyalty
/// counters on it and the player who cast it chose to pay life for any part of its
/// cost represented by Phyrexian mana symbols, it instead enters the battlefield
/// with that many loyalty counters minus two for each of those mana symbols.'"
/// </para>
///
/// <para>
/// MAST records the keyword's presence as a parameterless <see cref="KeywordAbilityEffect"/>;
/// the loyalty-counter reduction logic is engine territory (the descriptive-not-engine
/// doctrine). The parenthetical reminder text explains the Phyrexian mana payment and
/// loyalty reduction; it is stripped by the classifier before the combinator fires,
/// but re-attached to the ability's <see cref="StaticAbility.Reminder"/> field.
/// </para>
/// </summary>
[Keyword]
public sealed class CompleatKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Compleated",
      RuleReference = "702.150",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.Compleated,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Compleated }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Compleated")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Compleated,
      Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Compleated }],
      Reminder = reminder,
    }
  );
}
