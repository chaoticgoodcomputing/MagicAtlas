namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Morph {cost}: The player may cast this card face down as a 2/2 colorless creature for {3},
/// and may turn it face up later by paying its morph cost.
/// Rule 702.37. MAST records the keyword and the morph cost; the cast-face-down rules and
/// turn-face-up mechanics are engine territory.
///
/// <para>
/// Combinator-only keyword (no <see cref="KeywordDefinition"/>): Morph has no
/// <c>KeywordDefinitions.Morph</c> entry in the legacy file; only the parser combinator
/// lived in <c>OracleParsers</c>.
/// </para>
///
/// <para>
/// Two printed cost shapes share the "Morph" keyword slot: a mana cost printed directly
/// after the keyword ("Morph {1}{U}"), and a non-mana cost printed after an em dash
/// ("Morph—Reveal a red card in your hand."), per CR 702.37a: "'Morph [cost]' means 'You
/// may cast this card as a 2/2 face-down creature with no text, no name, no subtypes, and
/// no mana cost by paying {3} rather than paying its mana cost.'" The {3} face-down cast
/// cost is fixed by the rule and is not itself recorded here (per the
/// descriptive-not-engine doctrine); <see cref="MorphEffect.Cost"/> records only the
/// printed morph (turn-face-up) cost, mana or non-mana.
/// </para>
/// </summary>
[Keyword]
public sealed class MorphKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <summary>
  /// Maps a color name word ("red", "blue", …) to its WUBRG code. Returns null for any
  /// other word so the combinator can fail cleanly rather than match a wrong color.
  /// </summary>
  private static string? ColorCode(string word) => word.ToLowerInvariant() switch
  {
    "white" => "W",
    "blue" => "U",
    "black" => "B",
    "red" => "R",
    "green" => "G",
    _ => null,
  };

  private static readonly TokenListParser<OracleToken, string> ColorWord = Token
    .EqualTo(OracleToken.Word)
    .Try()
    .Where(t => ColorCode(t.ToStringValue()) != null)
    .Select(t => ColorCode(t.ToStringValue())!);

  // "Morph {cost}" — the mana-cost variant.
  private static readonly TokenListParser<OracleToken, Ability> _manaCostMorph = (
    from keyword in Keyword("Morph")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Morph,
      Effects = [new MorphEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );

  // "Morph—Reveal a [color] card in your hand." — the non-mana reveal-cost variant.
  private static readonly TokenListParser<OracleToken, Ability> _revealCostMorph = (
    from keyword in Keyword("Morph")
    from emDash in Token.EqualTo(OracleToken.EmDash)
    from revealWord in Keyword("Reveal")
    from article in Keyword("a")
    from color in ColorWord
    from cardWord in Keyword("card")
    from inWord in Keyword("in")
    from yourWord in Token.EqualTo(OracleToken.Your)
    from handWord in Keyword("hand")
    from period in Token.EqualTo(OracleToken.Period)
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Morph,
      Effects = [new MorphEffect
      {
        Cost = new RevealCost
        {
          Filter = new ObjectFilter
          {
            CardTypes = ["card"],
            Colors = [color],
          },
          Quantity = LiteralQuantity.Of(1),
          Zone = Zone.Hand,
        },
      }],
      Reminder = reminder,
    }
  );

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } =
    _manaCostMorph.Try().Or(_revealCostMorph.Try());
}
