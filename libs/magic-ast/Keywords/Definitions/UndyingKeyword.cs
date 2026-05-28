namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Undying: When this creature dies, if it had no +1/+1 counters on it,
/// return it to the battlefield under its owner's control with a +1/+1 counter on it.
/// Rule 702.93. Mirror of Persist (Rule 702.78) with opposite polarity:
/// Persist checks for no -1/-1 counters; Undying checks for no +1/+1 counters.
/// MAST records keyword presence; the dies-trigger, counter-check, and
/// return-to-battlefield semantics are engine territory.
/// </summary>
[Keyword]
public sealed class UndyingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Undying",
      RuleReference = "702.93",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Undying",
        Effects = [new UndyingEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Undying")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Undying",
      Effects = [new UndyingEffect { IsOptional = false }],
      Reminder = reminder,
    }
  );
}
