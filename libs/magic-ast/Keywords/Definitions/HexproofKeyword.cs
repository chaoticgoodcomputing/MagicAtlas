namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Hexproof: This permanent can't be the target of spells or abilities your opponents
/// control. Rule 702.11. MAST records keyword presence.
/// </summary>
[Keyword]
public sealed class HexproofKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Hexproof")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Hexproof",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Hexproof }],
      Reminder = reminder,
    }
  );
}
