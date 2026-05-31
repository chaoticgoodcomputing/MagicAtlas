namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Escape (Rule 702.138): "Escape—[cost], Exile N other cards from your graveyard."
/// An alternative cost on instants and sorceries that lets the card be cast from the
/// graveyard. The escape cost has two printed halves — a mana cost and the additional
/// cost of exiling a fixed number of other cards from the controller's graveyard. MAST
/// records both halves; the cast-from-graveyard permission is inferred from the rules
/// and captured as the reminder parenthetical. Combinator-only: no <c>KeywordDefinition</c>
/// entry exists.
///
/// <para>
/// The exile count is part of the COST. The graveyard exile is deliberately recorded as a
/// count (<see cref="EscapeEffect.CardsToExile"/>), not modelled as a separate
/// <c>ExileEffect</c> subtree — MAST describes the cost, it does not execute it.
/// </para>
/// </summary>
[Keyword]
public sealed class EscapeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <summary>
  /// Maps a word-number / numeric-literal token to a <see cref="LiteralQuantity"/>.
  /// Mirrors the word-number table used elsewhere in the parser (ModalAbilityParser).
  /// Defined privately here so the Escape file owns its number parsing without touching
  /// the shared <c>KeywordCombinators</c>.
  /// </summary>
  private static readonly TokenListParser<OracleToken, Quantity> ExileCount = Token
    .Matching<OracleToken>(k => k == OracleToken.WordNumber || k == OracleToken.Number, "card count")
    .Select(t => (Quantity)LiteralQuantity.Of(ParseCount(t.ToStringValue())));

  /// <summary>
  /// Consumes the descriptive tail of the exile clause — every token up to (and including)
  /// the sentence-terminating period — without structuring it. The escape cost's exile
  /// count is the only structured datum; "other cards from your graveyard" is descriptive
  /// boilerplate that varies only cosmetically.
  /// </summary>
  private static readonly TokenListParser<OracleToken, Token<OracleToken>> ExileClauseTail = Token
    .Matching<OracleToken>(k => k != OracleToken.Period, "exile clause text")
    .Many()
    .IgnoreThen(Token.EqualTo(OracleToken.Period));

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Escape")
    from emDash in Token.EqualTo(OracleToken.EmDash)
    from cost in ManaCostSymbols
    from comma in Token.EqualTo(OracleToken.Comma)
    from exileWord in Keyword("Exile")
    from count in ExileCount
    from tail in ExileClauseTail
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Escape,
      Effects = [new EscapeEffect { Cost = cost, CardsToExile = count }],
      Reminder = reminder,
    }
  );

  private static int ParseCount(string token)
  {
    if (int.TryParse(token, out var literal))
    {
      return literal;
    }
    return token.ToLowerInvariant() switch
    {
      "zero" => 0,
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => 0,
    };
  }
}
