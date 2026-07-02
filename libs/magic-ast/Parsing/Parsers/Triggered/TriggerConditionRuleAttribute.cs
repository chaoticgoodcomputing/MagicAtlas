namespace MagicAST.Parsing.Parsers.Triggered;

/// <summary>
/// Marks an <see cref="ITriggerConditionRule"/> implementation for
/// reflection-based discovery by <see cref="TriggeredAbilityParser"/>. Mirrors
/// <see cref="TriggeredRuleAttribute"/> for the trigger-condition side.
/// </summary>
/// <remarks>
/// Phase-4 priorities are migrated ORDER-PRESERVING from the legacy guarded
/// chain as <c>Priority = 1000 - (index in the legacy ParseTriggerCondition
/// chain)</c>, so an extracted rule keeps the exact relative dispatch order it
/// had as an <c>if</c>-guard. See
/// <see cref="MagicAST.Parsing.Parsers.IPrioritizedRuleAttribute"/> for the
/// shared priority contract (higher = more specific = tried first).
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TriggerConditionRuleAttribute : Attribute, MagicAST.Parsing.Parsers.IPrioritizedRuleAttribute
{
  public int Priority { get; init; } = 50;
}
