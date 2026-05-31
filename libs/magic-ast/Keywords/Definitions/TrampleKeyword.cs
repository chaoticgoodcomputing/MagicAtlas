namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Trample: If this creature would assign enough damage to its blockers to
/// destroy them, you may have it assign the rest of its damage to the
/// defending player or planeswalker. Rule 702.19. MAST records keyword
/// presence; the excess-damage assignment is engine territory.
///
/// <para>
/// Simple parameterless keyword — combinator-only (no KeywordDefinition in
/// the legacy registry). Tier: Simple.
/// </para>
/// </summary>
[Keyword]
public sealed class TrampleKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Trample")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Trample",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Trample }],
      Reminder = reminder,
    }
  );
}
