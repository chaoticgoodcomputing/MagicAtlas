namespace MagicAST.Parsing.Parsers.Static;

/// <summary>
/// Marks an <see cref="IStaticRule"/> implementation for reflection-based discovery
/// by <see cref="StaticAbilityParser"/>. Each decorated rule contributes one
/// oracle-text recognizer to the parser's static-ability cascade.
///
/// <para>
/// <b>Priority convention (range 0–100, default 50, higher = more specific = tried
/// first):</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>80–100</b> — rules that supersede a more-general sibling
///   (color-disjunction before type-disjunction, IfYouDo variants before plain
///   variants, "this creature" before "a [type]"). Without the bump these would
///   never fire because the more-general rule would shadow them.</description></item>
///   <item><description><b>50</b> — default. Non-overlapping rules whose regex
///   anchors are mutually exclusive.</description></item>
///   <item><description><b>20–40</b> — catch-all / generic rules that should be
///   tried last so a more-specific sibling gets a chance first.</description></item>
/// </list>
///
/// <para>
/// The numeric scheme replaces the old source-order convention: each rule used to
/// declare its priority implicitly by where it appeared in <c>TryParse</c>'s
/// cascade. Splitting rules into one-file-per-rule loses that ordering signal, so
/// each rule now states its priority explicitly.
/// </para>
///
/// <para>
/// <b>Phase 3 migration note:</b> priorities are migrated ORDER-PRESERVING as
/// <c>Priority = 1000 - (index in the legacy dispatch chain)</c>, so the original
/// first-non-null-wins order of <see cref="StaticAbilityParser"/>'s legacy chain is
/// reproduced exactly by construction. (The 0–100 band convention above describes
/// the intended steady-state once the migration settles; until then the literal
/// 1000-minus-index values keep behavior identical.)
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class StaticRuleAttribute : Attribute, MagicAST.Parsing.Parsers.IPrioritizedRuleAttribute
{
  /// <summary>
  /// Dispatch priority. Higher = tried first. See type-level docs for the band
  /// convention. Default <c>50</c> when omitted.
  /// </summary>
  public int Priority { get; init; } = 50;
}
