namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Reach: This creature can block creatures with flying.
/// Rule 702.17. Parameterless keyword marker — no KeywordDefinition entry in the legacy
/// registry; combinator only.
/// </summary>
[Keyword]
public sealed class ReachKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Reach")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Reach",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Reach }],
      Reminder = reminder,
    }
  );
}
