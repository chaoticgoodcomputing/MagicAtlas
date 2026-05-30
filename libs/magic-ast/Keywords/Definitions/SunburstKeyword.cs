namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Sunburst: This permanent enters with a +1/+1 counter on it for each color
/// of mana spent to cast it. (Non-creature artifacts use charge counters
/// instead.) Rule 702.44. MAST records keyword presence; the color-counting
/// and counter-placement are engine territory.
/// </summary>
[Keyword]
public sealed class SunburstKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Sunburst",
      RuleReference = "702.44",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Sunburst",
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Sunburst }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Sunburst")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Sunburst",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Sunburst }],
      Reminder = reminder,
    }
  );
}
