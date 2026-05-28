namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Persist: When this creature dies, if it had no -1/-1 counters on it,
/// return it to the battlefield under its owner's control with a -1/-1 counter on it.
/// Rule 702.78. MAST records keyword presence; the dies-trigger and counter-placement
/// semantics are engine territory.
/// </summary>
[Keyword]
public sealed class PersistKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Persist")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Persist",
      Effects = [new PersistEffect { IsOptional = false }],
      Reminder = reminder,
    }
  );
}
