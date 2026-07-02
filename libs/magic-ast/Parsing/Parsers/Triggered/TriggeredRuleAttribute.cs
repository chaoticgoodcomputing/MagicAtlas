namespace MagicAST.Parsing.Parsers.Triggered;

/// <summary>
/// Marks an <see cref="ITriggeredRule"/> implementation for reflection-based
/// discovery by <see cref="TriggeredAbilityParser"/>. See
/// <see cref="MagicAST.Parsing.Parsers.Spell.SpellRuleAttribute"/> for the
/// shared priority convention (0–100, default 50, higher = more specific = tried first).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TriggeredRuleAttribute : Attribute, MagicAST.Parsing.Parsers.IPrioritizedRuleAttribute
{
  public int Priority { get; init; } = 50;
}
