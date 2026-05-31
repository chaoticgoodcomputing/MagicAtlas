namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Awaken N—[cost]. Rule 702.113. An alternative cost printed on instants and
/// sorceries: "Awaken N—[cost]". If the spell is cast for its awaken cost, it
/// additionally puts N +1/+1 counters on a target land you control and animates
/// that land. MAST records the keyword, the awaken number N, and the alternative
/// cost; the counters-on-land / land-becomes-creature semantics are engine
/// territory (and echoed in reminder text). Combinator-only: no matching
/// <c>KeywordDefinitions</c> entry exists in the legacy registry.
///
/// <para>
/// Mirrors the Suspend "N—[cost]" shape (<see cref="AwakenEffect.N"/> +
/// <see cref="AwakenEffect.Cost"/>). The number/em-dash glue is parsed by private
/// combinators in this file; the trailing cost reuses the shared
/// <c>ManaCostSymbols</c> primitive.
/// </para>
/// </summary>
[Keyword]
public sealed class AwakenKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Awaken")
    from n in AwakenNumber
    from dash in EmDash
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Awaken",
      Effects = [new AwakenEffect
      {
        N = n,
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parses the "N" in "Awaken N—[cost]" into a <see cref="LiteralQuantity"/>.
  /// </summary>
  private static readonly TokenListParser<OracleToken, Quantity> AwakenNumber = Token
    .EqualTo(OracleToken.Number)
    .Select(t => (Quantity)LiteralQuantity.Of(int.Parse(t.ToStringValue())));

  /// <summary>
  /// Parses the em-dash glue between the awaken number and its cost.
  /// </summary>
  private static readonly TokenListParser<OracleToken, Token<OracleToken>> EmDash =
    Token.EqualTo(OracleToken.EmDash);
}
