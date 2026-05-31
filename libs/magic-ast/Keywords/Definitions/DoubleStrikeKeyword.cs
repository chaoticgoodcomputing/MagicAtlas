namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Double Strike: This creature deals both first-strike and regular combat damage.
/// Rule 702.4.
/// </summary>
[Keyword]
public sealed class DoubleStrikeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Double strike",
      RuleReference = "702.4",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Double strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from double_ in Keyword("Double")
    from strike in Keyword("Strike").Or(Keyword("strike"))
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Double strike",
      Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      Reminder = reminder,
    }
  );
}
