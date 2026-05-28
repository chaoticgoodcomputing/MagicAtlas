namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Choose a Background — Commander Legends: Battle for Baldur's Gate partner variant
/// (Rule 702.124g). Emits a static ability whose effect carries
/// <see cref="PartnerType.ChooseABackground"/>. Combinator-only: no matching
/// <c>KeywordDefinitions</c> entry exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class ChooseABackgroundKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from choose in Token.EqualTo(OracleToken.Choose)
    from a in Keyword("a")
    from background in Keyword("Background")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Choose a Background",
      Effects = [new PartnerEffect { PartnerType = PartnerType.ChooseABackground }],
      Reminder = reminder,
    }
  );
}
