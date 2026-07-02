namespace MagicAST.Parsing.Parsers.Activated;

/// <summary>
/// Marks an <see cref="IActivatedCostRule"/> implementation for reflection-based
/// discovery by <see cref="ActivatedAbilityParser"/>. Priorities are migrated
/// order-preserving from the legacy <c>ParseCosts</c> chain as
/// <c>Priority = 1000 - (chain index)</c>. See
/// <see cref="MagicAST.Parsing.Parsers.IPrioritizedRuleAttribute"/> for the shared
/// priority contract (higher = more specific = tried first).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ActivatedCostRuleAttribute : Attribute, MagicAST.Parsing.Parsers.IPrioritizedRuleAttribute
{
  public int Priority { get; init; } = 50;
}
