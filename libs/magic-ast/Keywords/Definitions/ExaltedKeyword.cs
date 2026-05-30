namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Exalted: Whenever a creature you control attacks alone, that creature gets +1/+1
/// until end of turn. Rule 702.83. Although mechanically a triggered ability, MAST
/// models it as a keyword marker (same approach as Prowess); the trigger-and-buff
/// expansion is engine territory.
/// </summary>
[Keyword]
public sealed class ExaltedKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Exalted")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Exalted",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Exalted }],
      Reminder = reminder,
    }
  );
}
