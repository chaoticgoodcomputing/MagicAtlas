namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Shroud: This permanent can't be the target of spells or abilities (including
/// by its controller). Rule 702.18. MAST records keyword presence.
/// </summary>
[Keyword]
public sealed class ShroudKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Shroud")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Shroud",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Shroud }],
      Reminder = reminder,
    }
  );
}
