namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Suspend N—[cost] (Rule 702.62). A keyword representing three abilities — a
/// static "Suspend N—[cost]" ability functioning in hand, a triggered upkeep
/// ability removing a time counter, and a triggered ability playing the card
/// when the last time counter is removed. MAST records the keyword plus the
/// printed N (time counters) and cost; the three sub-abilities are inferred from
/// the rules and carried in the <see cref="Reminder"/>. Reuses the existing
/// <see cref="SuspendEffect"/>. Combinator-only: no <c>KeywordDefinition</c>.
/// </summary>
[Keyword]
public sealed class SuspendKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <summary>
  /// Parses a numeric literal token into a <see cref="LiteralQuantity"/>. Local to
  /// this keyword so <c>KeywordCombinators.cs</c> stays untouched.
  /// </summary>
  private static readonly TokenListParser<OracleToken, Quantity> NumberLiteral = Token
    .EqualTo(OracleToken.Number)
    .Select(t => (Quantity)LiteralQuantity.Of(int.Parse(t.ToStringValue())));

  /// <summary>Parses the em-dash separating "Suspend N" from the cost.</summary>
  private static readonly TokenListParser<OracleToken, Superpower.Model.Token<OracleToken>> Dash =
    Token.EqualTo(OracleToken.EmDash);

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Suspend")
    from n in NumberLiteral
    from dash in Dash
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Suspend",
      Effects = [new SuspendEffect
      {
        N = n,
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
