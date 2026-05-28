namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Fuse: You may cast one or both halves of this card from your hand.
/// Rule 702.102. Found on split cards from Dragon's Maze. MAST records the
/// keyword's presence; the split-card casting modes and cost-combination
/// mechanics are engine territory.
/// </summary>
[Keyword]
public sealed class FuseKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Fuse",
      RuleReference = "702.102",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Fuse",
        Effects = [new FuseEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Fuse")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Fuse",
      Effects = [new FuseEffect()],
      Reminder = reminder,
    }
  );
}
