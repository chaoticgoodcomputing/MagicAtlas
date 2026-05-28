namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Rebound: If you cast this spell from your hand, exile it as it resolves.
/// At the beginning of your next upkeep, you may cast this card from exile
/// without paying its mana cost.
/// Rule 702.88. Parameterless keyword marker.
/// </summary>
[Keyword]
public sealed class ReboundKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Rebound",
      RuleReference = "702.88",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Rebound",
        Effects = [new ReboundEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Rebound")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Rebound",
      Effects = [new ReboundEffect()],
      Reminder = reminder,
    }
  );
}
