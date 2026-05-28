namespace MagicAST.Keywords.Definitions;

using MagicAST.AST;
using MagicAST.AST.Costs;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;

/// <summary>
/// Shared combinator primitives for one-file-per-keyword <see cref="IKeyword"/>
/// implementations under <c>Keywords/Definitions/</c>. These are lifted from the
/// private helpers in <c>OracleParsers</c> so each keyword file can build its
/// combinator without depending on <c>OracleParsers</c> internals.
///
/// <para>
/// Behaviour-identical to the legacy private helpers: a keyword migrated to a file
/// produces the exact same <see cref="StaticAbility"/> as the combinator it shadows.
/// </para>
/// </summary>
public static class KeywordCombinators
{
  /// <summary>
  /// Parses a specific keyword token (case-insensitive word match). Mirrors
  /// <c>OracleParsers.Keyword(string)</c>.
  /// </summary>
  public static TokenListParser<OracleToken, Superpower.Model.Token<OracleToken>> Keyword(string keyword)
  {
    return Token
      .EqualTo(OracleToken.Word)
      .Try()
      .Where(t => t.ToStringValue().Equals(keyword, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Parses optional reminder text (parenthesized content). Mirrors
  /// <c>OracleParsers._optionalReminder</c>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, Parenthetical?> OptionalReminder = Token
    .EqualTo(OracleToken.ReminderText)
    .Select(t => (Parenthetical?)new Parenthetical { Text = t.ToStringValue() })
    .OptionalOrDefault();

  /// <summary>
  /// Parses one-or-more contiguous mana symbols into a <see cref="ManaCost"/>. Mirrors
  /// the inline mana-symbol matcher used by cost-parameterized keyword combinators in
  /// <c>OracleParsers</c> (Flashback, Cycling, Madness, …).
  /// </summary>
  public static readonly TokenListParser<OracleToken, ManaCost> ManaCostSymbols = Token
    .Matching<OracleToken>(
      k =>
        k == OracleToken.GenericMana
        || k == OracleToken.WhiteMana
        || k == OracleToken.BlueMana
        || k == OracleToken.BlackMana
        || k == OracleToken.RedMana
        || k == OracleToken.GreenMana
        || k == OracleToken.ColorlessMana
        || k == OracleToken.VariableMana
        || k == OracleToken.HybridMana
        || k == OracleToken.PhyrexianMana,
      "mana symbol"
    )
    .AtLeastOnce()
    .Select(costSymbols => new ManaCost
    {
      Symbols = costSymbols
        .Select(t => new ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
        .ToList(),
    });
}
