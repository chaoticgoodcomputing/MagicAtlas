namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Extort: Whenever you cast a spell, you may pay {W/B}. If you do, each opponent
/// loses 1 life and you gain that much life. Rule 702.101. MAST records the keyword's
/// presence; the spell-cast trigger and life-drain/gain are engine territory.
/// Parameterless keyword marker — mirrors AscendEffect, ConvokeEffect, EvolveEffect,
/// and PersistEffect.
/// </summary>
[Keyword]
public sealed class ExtortKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Extort")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Extort,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Extort }],
      Reminder = reminder,
    }
  );
}
