namespace MagicAST.Keywords;

using MagicAST.Parsing.Parsers;

/// <summary>
/// Marks an <see cref="IKeyword"/> implementation for reflection-based discovery by
/// <see cref="KeywordRegistry"/>. Each decorated file contributes one keyword's
/// definition (to the expander) and combinator (to the keyword Or-chain).
///
/// <para>
/// <b>Priority convention (range 0–100, default 50, higher = tried first within its
/// <see cref="KeywordTier"/>):</b> the registry folds discovered combinators into the
/// Simple and Parameterized Or-chains ordered by descending priority then ordinal
/// name. Superpower's <c>.Or</c> is first-success-wins, so a keyword whose oracle text
/// is a <i>prefix</i> of another must be tried <i>after</i> the longer one — bump the
/// longer/more-specific keyword's priority so it sorts earlier.
/// </para>
/// <list type="bullet">
///   <item><description><b>60–100</b> — keywords that must precede a sibling whose
///   token prefix they share. Examples: <c>Partner with [Name]</c> (must beat bare
///   <c>Partner</c>); multi-word <c>First strike</c> / <c>Double strike</c> before any
///   single-word keyword that could partially consume their first token. Without the
///   bump first-success-wins would let the shorter sibling shadow them.</description></item>
///   <item><description><b>50</b> — default. Non-overlapping keywords whose leading
///   token is unique (the vast majority — Flying, Toxic, Flashback, …).</description></item>
///   <item><description><b>0–40</b> — deliberately-last catch-alls (e.g. a generic
///   <c>[Type]cycling</c> matcher that should yield to specific cycling variants
///   first).</description></item>
/// </list>
///
/// <para>
/// The numeric scheme replaces the old source-order convention: ordering used to be
/// implicit in where a combinator appeared in the <c>SimpleKeyword</c> /
/// <c>ParameterizedKeyword</c> Or-chains. One-file-per-keyword loses that signal, so
/// each keyword now states its priority explicitly when ordering matters.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KeywordAttribute : Attribute, IPrioritizedRuleAttribute
{
  /// <summary>
  /// Intra-tier dispatch priority. Higher = folded earlier into the Or-chain. See
  /// type-level docs for the band convention. Default <c>50</c> when omitted.
  /// </summary>
  public int Priority { get; init; } = 50;
}
