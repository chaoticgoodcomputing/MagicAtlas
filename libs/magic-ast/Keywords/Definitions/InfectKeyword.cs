namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Infect: This creature deals damage to creatures in the form of -1/-1 counters and
/// to players in the form of poison counters. Rule 702.91. MAST records keyword
/// presence; the damage-redirection semantics are engine territory.
/// </summary>
[Keyword]
public sealed class InfectKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Infect")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Infect",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Infect }],
      Reminder = reminder,
    }
  );
}
