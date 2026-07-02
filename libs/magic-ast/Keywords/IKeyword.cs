namespace MagicAST.Keywords;

using MagicAST.AST.Abilities;
using MagicAST.Parsing.Tokens;
using Superpower;

/// <summary>
/// One self-contained keyword. Each implementation bundles the two facts that used to
/// be split across <c>KeywordDefinitions.cs</c> (the expansion definition) and
/// <c>OracleParsers.cs</c> (the oracle-text combinator) into a single file under
/// <c>Keywords/Definitions/</c>, decorated with <see cref="KeywordAttribute"/> for
/// reflection discovery by <see cref="KeywordRegistry"/>.
///
/// <para>
/// Splitting keyword knowledge one-file-per-keyword removes the two-hot-file
/// merge-conflict bottleneck: a new keyword (or a batch of them) is a set of new
/// files with no edits to any shared list or Or-chain.
/// </para>
/// </summary>
public interface IKeyword
{
  /// <summary>
  /// The expansion definition — the same <see cref="KeywordDefinition"/> instance that
  /// previously lived as a static property on <c>KeywordDefinitions</c>. Feeds the
  /// keyword-expander registry.
  ///
  /// <para>
  /// <c>null</c> for combinator-only keywords that never had a <c>KeywordDefinition</c>
  /// (e.g. Flashback, Conspire, Fuse — present in the legacy <c>OracleParsers</c> chain
  /// but absent from <c>KeywordDefinitions.All</c>). Such keywords still contribute their
  /// <see cref="Combinator"/> to the parse chain but nothing to the expander.
  /// </para>
  /// </summary>
  KeywordDefinition? Definition { get; }

  /// <summary>
  /// The oracle-text recognizer — the same combinator that previously lived as a static
  /// field on <c>OracleParsers</c>. Folded into the registry's keyword Or-chain.
  /// </summary>
  TokenListParser<OracleToken, Ability> Combinator { get; }

  /// <summary>
  /// Which Or-chain this keyword's combinator belongs to. Mirrors the legacy split
  /// between <c>OracleParsers.SimpleKeyword</c> and
  /// <c>OracleParsers.ParameterizedKeyword</c>.
  /// </summary>
  KeywordTier Tier { get; }
}

/// <summary>
/// The Or-chain a keyword's combinator folds into, mirroring the legacy
/// <c>SimpleKeyword</c> / <c>ParameterizedKeyword</c> split. <c>AnyKeyword</c> tries
/// <see cref="Simple"/> first, then <see cref="Parameterized"/>.
/// </summary>
public enum KeywordTier
{
  /// <summary>
  /// Parameterless keywords (e.g., Flying, First strike). Folded into the
  /// Simple Or-chain.
  /// </summary>
  Simple,

  /// <summary>
  /// Keywords carrying a parameter — number, mana cost, quality, name (e.g., Toxic,
  /// Flashback, Protection). Folded into the Parameterized Or-chain.
  /// </summary>
  Parameterized,
}
