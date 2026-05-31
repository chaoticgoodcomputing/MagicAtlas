namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Wither: This creature deals damage to creatures in the form of -1/-1 counters.
/// Rule 702.80. MAST records keyword presence; the damage-redirection semantics are
/// engine territory.
/// </summary>
[Keyword]
public sealed class WitherKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Wither")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Wither",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Wither }],
      Reminder = reminder,
    }
  );
}
