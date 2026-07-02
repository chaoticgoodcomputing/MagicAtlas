namespace MagicAST.Parsing.Parsers;

using MagicAST.AST.Abilities;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers.Static;
using MagicAST.Parsing.Tokens;

/// <summary>
/// Dispatches static-ability oracle-text clauses to the priority-ordered set of
/// <see cref="IStaticRule"/> implementations discovered by reflection at construction
/// time. Each rule lives in its own file under <c>Parsers/Static/Rules/</c> and is
/// decorated with <see cref="StaticRuleAttribute"/> (see attribute docs for the
/// order-preserving priority convention). Adding a new shape means dropping a new file
/// in <c>Rules/</c> with no edits to any shared file.
/// </summary>
/// <remarks>
/// Falls through to <see cref="FallbackParser"/> when no rule matches. Shared
/// recognition logic used by multiple rules lives in
/// <see cref="Static.StaticRuleHelpers"/>.
/// </remarks>
[OracleAbilityParser(AbilityKind.Static)]
public sealed class StaticAbilityParser : IAbilityParser
{
  private readonly FallbackParser _fallback = new();

  /// <summary>
  /// Reflection-discovered <see cref="IStaticRule"/> implementations, ordered by
  /// descending <see cref="StaticRuleAttribute.Priority"/> then ordinal name (see
  /// <see cref="RuleRegistry.Discover{TRule, TAttr}"/>). Priorities were migrated
  /// order-preserving from the original hand-ordered dispatch chain.
  /// </summary>
  private readonly IReadOnlyList<DiscoveredRule<IStaticRule>> _staticRules =
    RuleRegistry.Discover<IStaticRule, StaticRuleAttribute>("StaticAbilityParser");

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    var parsed = TryParse(clause, classification);
    if (parsed is { Count: > 0 })
    {
      return parsed;
    }
    return
    [
      _fallback.Parse(
        clause,
        classification,
        "Static ability parser not yet implemented",
        lastAttemptedRule: "StaticAbilityParser.Parse",
        failurePosition: clause.SourceSpan.Start
      ),
    ];
  }

  /// <summary>
  /// Attempts to parse static abilities from a clause by dispatching to the
  /// priority-ordered rule chain; first non-null result wins. Returns null when no
  /// rule recognises the clause.
  /// </summary>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    foreach (var entry in _staticRules)
    {
      var result = entry.Rule.TryParse(clause, classification);
      if (result is { Count: > 0 })
      {
        return result;
      }
    }
    return null;
  }
}
