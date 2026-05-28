namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Riot: This creature enters with your choice of a +1/+1 counter or haste.
/// Rule 702.138. Parameterless keyword marker.
/// </summary>
[Keyword]
public sealed class RiotKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Riot",
      RuleReference = "702.138",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Riot",
        Effects = [new RiotEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Riot")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Riot",
      Effects = [new RiotEffect()],
      Reminder = reminder,
    }
  );
}
