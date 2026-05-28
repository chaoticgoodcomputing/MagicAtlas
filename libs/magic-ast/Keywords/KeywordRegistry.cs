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
/// This is the registry half of the Phase-2 bridge: consumers try the registry first,
/// then fall back to the legacy <c>KeywordDefinitions</c> / <c>OracleParsers</c> content
/// (see <see cref="KeywordExpander.CreateDefault"/> and
/// <c>OracleParsers.KeywordList</c>). A keyword migrated to a file therefore shadows
/// its legacy twin without that twin being deleted yet — Stage B migrates the rest in
/// parallel, Stage C deletes the legacy content.
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
    _keywords.Select(k => k.Definition).ToList();

  /// <summary>
  /// The discovered-keyword per-element combinator: a Simple-then-Parameterized Or-chain
  /// mirroring <c>OracleParsers.AnyKeyword</c> = SimpleKeyword.Try().Or(ParameterizedKeyword),
  /// <i>before</i> any comma-list wrap. Each combinator is wrapped in <c>.Try()</c> so
  /// first-success-wins backtracks cleanly between candidates (preserving the legacy
  /// Or-chain semantics).
  ///
  /// <para>
  /// This is the splice point for the bridge: <c>OracleParsers.AnyKeyword</c> tries this
  /// first, then its legacy chain, so a migrated keyword's file shadows its legacy
  /// combinator while the comma-list machinery stays owned by <c>OracleParsers</c>
  /// (splicing here rather than at the list level avoids the
  /// <c>ManyDelimitedBy</c>-always-succeeds-with-empty trap that would block fallthrough
  /// for non-migrated keywords if the whole list were tried registry-first).
  /// </para>
  /// </summary>
  public static TokenListParser<OracleToken, StaticAbility> RegisteredAnyKeyword { get; } =
    BuildAnyKeyword();

  /// <summary>
  /// <see cref="RegisteredAnyKeyword"/> wrapped in <c>ManyDelimitedBy(Comma)</c>, exactly
  /// matching the <c>OracleParsers.KeywordList</c> shape. Provided as the registry's
  /// standalone keyword-list combinator; the live bridge splices
  /// <see cref="RegisteredAnyKeyword"/> rather than this whole list (see that property's
  /// docs for why).
  /// </summary>
  public static TokenListParser<OracleToken, IReadOnlyList<StaticAbility>> RegisteredKeywordList { get; } =
    RegisteredAnyKeyword
      .ManyDelimitedBy(Token.EqualTo(OracleToken.Comma))
      .Select(arr => (IReadOnlyList<StaticAbility>)arr);

  /// <summary>
  /// True when at least one keyword has been migrated to a file. Lets consumers skip
  /// the registry combinator entirely while the <c>Definitions/</c> folder is empty,
  /// rather than threading a guaranteed-fail parser into their Or-chain.
  /// </summary>
  public static bool HasRegisteredKeywords => _keywords.Count > 0;

  private static TokenListParser<OracleToken, StaticAbility> BuildAnyKeyword()
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
  private static TokenListParser<OracleToken, StaticAbility>? FoldTier(KeywordTier tier)
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
  private static TokenListParser<OracleToken, StaticAbility> AlwaysFail() =>
    input => TokenListParserResult.Empty<OracleToken, StaticAbility>(input, "no registered keywords");
}
