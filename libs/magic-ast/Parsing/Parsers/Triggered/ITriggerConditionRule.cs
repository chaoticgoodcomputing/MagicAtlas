namespace MagicAST.Parsing.Parsers.Triggered;

using MagicAST.AST.Triggers;

/// <summary>
/// One trigger-condition recognition rule (the event + filter half of a
/// triggered ability). Implementations are discovered by reflection via
/// <see cref="TriggerConditionRuleAttribute"/> and dispatched by
/// <see cref="TriggeredAbilityParser"/> in descending <c>Priority</c> order, then
/// ordinal name for tie-breaking; the first non-null result wins.
/// </summary>
/// <remarks>
/// Each rule checks its own guard internally and returns <c>null</c> when its
/// guard fails — there is no central keyword gate, so a rule is fully
/// self-contained (guard + recognizer live together). <paramref name="lower"/> is
/// the precomputed <c>triggerText.ToLowerInvariant()</c>, passed in so each rule
/// does not recompute it.
/// </remarks>
public interface ITriggerConditionRule
{
  /// <summary>
  /// Attempts to recognise this rule's trigger-condition shape.
  /// </summary>
  /// <param name="triggerText">The (case-preserving) trigger fragment.</param>
  /// <param name="lower">Precomputed <c>triggerText.ToLowerInvariant()</c>.</param>
  /// <param name="timing">The resolved trigger timing (When/Whenever/At).</param>
  /// <returns>The matched <see cref="TriggerCondition"/>, or <c>null</c> if this rule's guard fails.</returns>
  TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing);
}
