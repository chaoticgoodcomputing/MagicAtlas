namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Prototype {cost} — P/T (Rule 702.160, Rule 718). Printed e.g.
/// "Prototype {1}{B} — 1/1". A static keyword that records the alternative
/// prototype cost and the smaller-form power/toughness; the
/// alternative-characteristics casting rules are reminder text, not structure
/// (descriptive-not-engine doctrine). Combinator-only: no matching
/// <c>KeywordDefinition</c> entry exists.
/// </summary>
[Keyword]
public sealed class PrototypeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <summary>Matches the em dash separating the cost from the P/T (e.g. "… {1}{B} — 1/1").</summary>
  private static readonly TokenListParser<OracleToken, Token<OracleToken>> EmDash =
    Token.EqualTo(OracleToken.EmDash);

  /// <summary>Matches the slash separating power from toughness (e.g. "1/1").</summary>
  private static readonly TokenListParser<OracleToken, Token<OracleToken>> Slash =
    Token.EqualTo(OracleToken.Slash);

  /// <summary>Matches a numeric P/T component and lifts its raw printed string.</summary>
  private static readonly TokenListParser<OracleToken, string> Number =
    Token.EqualTo(OracleToken.Number).Select(t => t.ToStringValue());

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Prototype")
    from cost in ManaCostSymbols
    from _ in EmDash
    from power in Number
    from __ in Slash
    from toughness in Number
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Prototype,
      Effects = [new PrototypeEffect
      {
        Cost = cost,
        Power = power,
        Toughness = toughness,
      }],
      Reminder = reminder,
    }
  );
}
