namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "{COLOR} was spent to cast it" / "unless {COLOR} was spent to cast it" — a
/// resolution-time check of which mana colors paid the total cost of the
/// spell that became this permanent (Plaxmanta: "sacrifice it unless {G} was
/// spent to cast it").
///
/// <para>
/// CR 601.2f (verbatim): "The player determines the total cost of the spell.
/// Usually this is just the mana cost. ... Costs may include paying mana,
/// tapping permanents, sacrificing permanents, discarding cards, and so on.
/// ..." CR 601.2h (verbatim): "The player pays the total cost. First, they
/// pay all costs that don't involve random elements or moving objects from
/// the library to a public zone, in any order. Then they pay all remaining
/// costs in any order. Partial payments are not allowed. Unpayable costs
/// can't be paid." — mana is "spent to cast" at this step, and which colors
/// paid it is a fact fixed once the spell finishes being cast.
/// </para>
///
/// <para>
/// CR 707.10 (Dawnglow Infusion, verbatim): "... Dawnglow Infusion is a
/// sorcery that reads, 'You gain X life if {G} was spent to cast this spell
/// and X life if {W} was spent to cast it.' Because mana isn't an object, a
/// copy of Dawnglow Infusion won't cause you to gain any life, no matter what
/// mana was spent to cast the original spell." — the canonical CR usage of
/// "{color} was spent to cast" as a condition, confirming this is a fixed
/// historical fact about the casting event, not a payable cost or a
/// resolution-time choice.
/// </para>
///
/// <para>
/// This is deliberately NOT modelled as <c>PreventableEffect</c>/
/// <c>UnlessClause</c> (the Karoo/Wild-Leotau "unless you pay {COST}"
/// shape): that pairing is payment-only — it names a <c>Player</c> and a
/// <c>Cost</c> and implies a live decision made at resolution. "Unless {G}
/// was spent to cast it" offers no choice at resolution; it is a lookup
/// against a fact already fixed when the spell was cast (CR 601.2f–h).
/// <see cref="ManaSpentToCastCondition"/> instead composes with
/// <see cref="MagicAST.AST.Effects.Core.ConditionalEffect"/>: the sacrifice
/// is the <c>Then</c> branch, firing when the color was NOT spent.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): the engine reads which colors of
/// mana actually paid the spell's total cost; MAST does not pre-evaluate it.
/// </para>
/// </summary>
[ConditionKind("manaSpentToCast")]
public sealed record ManaSpentToCastCondition : Condition
{
  /// <summary>
  /// The single-letter mana color code checked — "W", "U", "B", "R", or "G".
  /// </summary>
  public required string Color { get; init; }

  /// <summary>
  /// The polarity of the check (mirrors <see cref="TriggeringObjectCounterCondition.Present"/>
  /// / <see cref="TriggeringAbilityIsManaCondition.IsManaAbility"/>): <c>true</c> for
  /// "if {COLOR} was spent to cast it"; <c>false</c> for the "unless {COLOR} was spent
  /// to cast it" negation (Plaxmanta) — the condition holds when the color was
  /// <em>not</em> spent.
  /// </summary>
  public required bool WasSpent { get; init; }
}
