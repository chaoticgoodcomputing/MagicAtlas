namespace MagicAST.Parsing.Combinators;

using MagicAST.AST.Abilities;
using MagicAST.Keywords;
using MagicAST.Parsing.Tokens;
using Superpower;

/// <summary>
/// Keyword-ability parser combinators for Magic: The Gathering oracle text.
///
/// <para>
/// As of Phase-2 Stage C this type is a thin facade over
/// <see cref="KeywordRegistry"/>: the per-keyword combinators and the
/// <c>SimpleKeyword</c> / <c>ParameterizedKeyword</c> Or-chains that used to live here
/// have moved to one-file-per-keyword <see cref="MagicAST.Keywords.IKeyword"/>
/// implementations under <c>Keywords/Definitions/</c>, discovered by the registry.
/// The two members below preserve the public call shape
/// (<c>OracleParsers.KeywordList(tokens)</c> from <c>StaticAbilityParser</c>) while
/// delegating the actual recognition to the registry.
/// </para>
/// </summary>
public static class OracleParsers
{
  /// <summary>
  /// Parses any keyword ability (simple or parameterized), backed solely by the
  /// reflection-discovered registry chain.
  /// </summary>
  public static TokenListParser<OracleToken, StaticAbility> AnyKeyword =>
    KeywordRegistry.RegisteredAnyKeyword;

  /// <summary>
  /// Parses a comma-separated list of keyword abilities (e.g. "Flying, vigilance,
  /// trample"), backed solely by the registry. Invoked as
  /// <c>OracleParsers.KeywordList(tokens)</c> from
  /// <see cref="MagicAST.Parsing.Parsers.StaticAbilityParser"/>.
  /// </summary>
  public static TokenListParser<OracleToken, IReadOnlyList<StaticAbility>> KeywordList =>
    KeywordRegistry.RegisteredKeywordList;
}
