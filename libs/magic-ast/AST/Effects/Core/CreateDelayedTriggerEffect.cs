namespace MagicAST.AST.Effects.Core;

using MagicAST.AST.Abilities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// An effect that, on resolution, creates a <see cref="DelayedTriggeredAbility"/>
/// (CR 603.7) — the structured home for "…at the beginning of the next end step",
/// "Whenever you cast a creature spell this turn, …", etc. Replaces the former
/// practice of hanging an <c>AtBeginningOfNext*</c> duration on a one-shot effect:
/// a delayed trigger is an ability that fires later, not a duration on the effect
/// that set it up (ADR 0002/0004).
/// </summary>
[OracleEffect("createDelayedTrigger")]
public sealed record CreateDelayedTriggerEffect : Effect
{
  /// <summary>The delayed triggered ability created when this effect resolves.</summary>
  public required DelayedTriggeredAbility DelayedTrigger { get; init; }
}
