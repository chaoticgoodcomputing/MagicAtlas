namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Cost increase effect: raises the cost to cast spells that match a targeting
/// condition. The prototypical oracle form is the pre-Ward pattern:
/// "Spells your opponents cast that target this creature cost {N} more to cast."
/// (Rule 117.6 — some effects add costs, making a spell more expensive.)
///
/// <para>
/// This is a descriptive static property, not a triggered ability. It differs
/// from the modern Ward keyword (which triggers on targeting and then counters
/// unless cost is paid). The two are functionally similar but structurally
/// distinct — Ward is a triggered ability; this effect is a continuous cost
/// modification.
/// </para>
///
/// <para>
/// <see cref="Amount"/> carries the generic-mana increase (e.g., 2 for "{2}
/// more"). <see cref="TargetedObject"/> records what the affected spells must
/// target (usually Self). <see cref="CasterFilter"/> records whose spells are
/// affected (usually Opponent).
/// </para>
/// </summary>
[OracleEffect("costIncrease")]
public sealed record CostIncreaseEffect : Effect
{
  /// <summary>
  /// The additional cost (in generic mana) imposed on affected spells.
  /// </summary>
  public required Quantity Amount { get; init; }

  /// <summary>
  /// The object that affected spells must target for the increase to apply.
  /// Typically <see cref="ObjectReferenceKind.Self"/> ("this creature").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? TargetedObject { get; init; }

  /// <summary>
  /// Filter on whose spells are affected. Typically
  /// <see cref="ControllerFilter.Opponent"/> ("your opponents cast").
  /// When null, all spells are affected regardless of who casts them.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? CasterFilter { get; init; }
}
