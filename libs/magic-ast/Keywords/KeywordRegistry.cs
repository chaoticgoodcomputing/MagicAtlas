namespace MagicAST.Keywords;

using MagicAST.AST.Abilities;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

/// <summary>
/// Reflection-discovered registry of one-file-per-keyword <see cref="IKeyword"/>
/// implementations under <c>Keywords/Definitions/</c>. Rebuilds the two artifacts a
/// keyword used to contribute across two hot files:
/// <list type="bullet">
///   <item><description><see cref="RegisteredDefinitions"/> — the expander's
///   definition list (was <c>KeywordDefinitions.All</c>).</description></item>
///   <item><description><see cref="RegisteredKeywordList"/> — the keyword Or-chain
///   combinator (was <c>OracleParsers.KeywordList</c>).</description></item>
/// </list>
///
/// <para>
/// As of Phase-2 Stage C this registry is the sole source of keyword knowledge: the
/// legacy <c>KeywordDefinitions</c> static-property list and the per-keyword
/// <c>OracleParsers</c> combinators have been deleted.
/// <see cref="KeywordExpander.CreateDefault"/> and <c>OracleParsers.KeywordList</c>
/// both delegate here directly — no legacy fallback remains.
/// </para>
/// </summary>
public static class KeywordRegistry
{
  private static readonly IReadOnlyList<IKeyword> _keywords =
    RuleRegistry
      .Discover<IKeyword, KeywordAttribute>("Keyword")
      .Select(d => d.Rule)
      .ToList();

  /// <summary>
  /// All keyword definitions discovered from files, ranked by the same descending
  /// priority + ordinal-name order the combinator chain uses (order is irrelevant for
  /// the expander's name-keyed dictionary, but kept consistent for determinism).
  /// </summary>
  public static IReadOnlyList<KeywordDefinition> RegisteredDefinitions { get; } =
    _keywords.Select(k => k.Definition).OfType<KeywordDefinition>().ToList();

  /// <summary>
  /// The discovered-keyword per-element combinator: a Simple-then-Parameterized Or-chain
  /// = <c>SimpleKeyword.Try().Or(ParameterizedKeyword)</c>, <i>before</i> any comma-list
  /// wrap. Each combinator is wrapped in <c>.Try()</c> so first-success-wins backtracks
  /// cleanly between candidates. <c>OracleParsers.AnyKeyword</c> delegates to this.
  /// </summary>
  public static TokenListParser<OracleToken, Ability> RegisteredAnyKeyword { get; } =
    BuildAnyKeyword();

  /// <summary>
  /// <see cref="RegisteredAnyKeyword"/> wrapped in <c>ManyDelimitedBy(Comma)</c>, the
  /// registry's keyword-list combinator. <c>OracleParsers.KeywordList</c> delegates to
  /// this.
  /// </summary>
  public static TokenListParser<OracleToken, IReadOnlyList<Ability>> RegisteredKeywordList { get; } =
    RegisteredAnyKeyword
      .ManyDelimitedBy(Token.EqualTo(OracleToken.Comma))
      .Select(arr => (IReadOnlyList<Ability>)arr);

  private static TokenListParser<OracleToken, Ability> BuildAnyKeyword()
  {
    var simple = FoldTier(KeywordTier.Simple);
    var parameterized = FoldTier(KeywordTier.Parameterized);

    if (simple is null)
    {
      return parameterized ?? AlwaysFail();
    }
    if (parameterized is null)
    {
      return simple;
    }
    // AnyKeyword = SimpleKeyword.Try().Or(ParameterizedKeyword) — mirrors OracleParsers.
    return simple.Try().Or(parameterized);
  }

  /// <summary>
  /// Folds every discovered combinator in <paramref name="tier"/> (already ranked by
  /// descending priority then ordinal name) into a single first-success-wins Or-chain,
  /// each candidate wrapped in <c>.Try()</c> for clean backtracking. Returns null when
  /// the tier has no migrated keywords (so the caller can elide it).
  /// </summary>
  private static TokenListParser<OracleToken, Ability>? FoldTier(KeywordTier tier)
  {
    var combinators = _keywords
      .Where(k => k.Tier == tier)
      .Select(k => k.Combinator.Try())
      .ToList();

    if (combinators.Count == 0)
    {
      return null;
    }

    var chain = combinators[0];
    for (var i = 1; i < combinators.Count; i++)
    {
      chain = chain.Or(combinators[i]);
    }
    return chain;
  }

  /// <summary>
  /// A combinator that always fails without consuming input — the identity element for
  /// an empty registry, so <see cref="RegisteredKeywordList"/> stays type-valid even
  /// before any keyword migrates.
  /// </summary>
  private static TokenListParser<OracleToken, Ability> AlwaysFail() =>
    input => TokenListParserResult.Empty<OracleToken, Ability>(input, "no registered keywords");
}
