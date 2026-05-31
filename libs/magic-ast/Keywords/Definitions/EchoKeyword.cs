namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Echo [cost]: At the beginning of your upkeep, if this permanent came under
/// your control since the beginning of your last upkeep, sacrifice it unless
/// you pay [cost].
/// Rule 702.30. MAST records the keyword and the echo cost; the
/// upkeep-trigger / sacrifice-unless-pay semantics are engine territory.
/// Combinator-only: no KeywordDefinition entry in the legacy registry.
/// </summary>
[Keyword]
public sealed class EchoKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Echo")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Echo",
      Effects = [new EchoEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
