namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.AST.Triggers;

/// <summary>
/// A triggered ability created by a <i>resolving effect</i> rather than printed on
/// the card (CR 603.7) — "Sacrifice it at the beginning of the next end step",
/// "Whenever you cast a creature spell this turn, draw a card". Effect-owned: it is
/// reached only through <see cref="MagicAST.AST.Effects.Core.CreateDelayedTriggerEffect"/>,
/// never appears in <c>CardOracle.Abilities</c>, and is deliberately <b>not</b> an
/// <see cref="AbilityKind"/> (which maps to CR 113.3's printed categories). See ADR 0002/0004.
///
/// <para>
/// Reuses <see cref="TriggerCondition"/> so its firing point is the same
/// <c>event | GameTime</c> union a printed trigger uses — covering both the "at"
/// (clock) and "when/whenever" (event) delayed forms. An optional
/// <see cref="Window"/> bounds a repeating delayed trigger to a span ("…this turn").
/// </para>
/// </summary>
public sealed record DelayedTriggeredAbility
{
  /// <summary>What fires the delayed ability — a clock point or an event (CR 603.7).</summary>
  public required TriggerCondition Trigger { get; init; }

  /// <summary>Optional span the delayed trigger is active for ("…this turn"); null for a one-shot.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Window { get; init; }

  /// <summary>The effects that occur when the delayed ability resolves.</summary>
  public required IReadOnlyList<Effect> Effects { get; init; }
}
