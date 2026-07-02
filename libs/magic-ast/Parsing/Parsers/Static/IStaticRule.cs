namespace MagicAST.Parsing.Parsers.Static;

using MagicAST.AST.Abilities;
using MagicAST.Parsing;

/// <summary>
/// One static-ability recognition rule. Each implementation maps a single
/// oracle-text shape to the <see cref="Ability"/> list it produces (almost always
/// a single <see cref="StaticAbility"/>, occasionally a keyword-list expansion).
/// Rules are discovered by reflection at <see cref="StaticAbilityParser"/>
/// construction via the <see cref="StaticRuleAttribute"/> decoration and dispatched
/// in descending <see cref="StaticRuleAttribute.Priority"/> order, with ordinal-name
/// order breaking ties within a band — first non-null (non-empty) result wins.
/// </summary>
/// <remarks>
/// The dispatcher falls through to the existing legacy chain (and ultimately
/// <see cref="FallbackParser"/>) when no rule matches. <paramref name="classification"/>
/// is threaded through because a small number of rules — the granted-ability and
/// as-long-as-grant shapes — need the clause's classification to recurse into the
/// inner ability parsers; most rules ignore it.
/// </remarks>
public interface IStaticRule
{
  /// <summary>
  /// Attempts to match <paramref name="clause"/>. Returns the produced ability
  /// list on a successful match; returns <c>null</c> (or an empty list) to decline,
  /// letting the dispatcher try the next rule.
  /// </summary>
  IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification);
}
