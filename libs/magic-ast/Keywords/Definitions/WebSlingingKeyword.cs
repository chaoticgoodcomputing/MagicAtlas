namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Web-slinging [cost]: You may cast this spell by paying [cost] and returning
/// a tapped creature you control to its owner's hand rather than paying its
/// mana cost.
/// Rule 702.188. Scope: mana-cost parameter. The return-a-tapped-creature
/// component of the alt cost is engine territory — MAST records the keyword's
/// presence and cost only, mirroring the Cycling/Evoke pattern.
/// Combinator-only: no matching <c>KeywordDefinitions</c> entry exists in the
/// legacy registry.
/// </summary>
[Keyword]
public sealed class WebSlingingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Web-slinging")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Web-slinging",
      Effects = [new WebSlingingEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
