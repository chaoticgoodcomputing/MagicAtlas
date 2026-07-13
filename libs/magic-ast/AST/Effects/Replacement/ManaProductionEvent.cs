namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Mana-production event: "you tap a permanent for mana, it produces [mana]" — the
/// replaceable event watched by mana-multiplication effects such as Nyxbloom Ancient
/// ("If you tap a permanent for mana, it produces three times as much of that mana
/// instead."), Mana Reflection / Zhur-Taa Ancient (double) and the Doubling Cube family.
///
/// CR 106.12: "To 'tap [a permanent] for mana' is to activate a mana ability of that
/// permanent that includes the {T} symbol in its activation cost." The mana that permanent
/// would produce is the event; a <see cref="ReplacementEffect"/> with a scaling
/// <see cref="ReplacementModifier"/> ("double"/"triple") multiplies the amount produced
/// (CR 605 — mana abilities; CR 614.1 — replacement effects apply continuously as events
/// happen). This is a REPLACEMENT event, not a triggered ability: the sibling
/// <c>TapForManaConditionRule</c> handles the trigger-side "Whenever you tap a permanent
/// for mana, …" shape (which ADDS mana on top via a separate ability); this node is the
/// mana that would be produced being replaced by a larger amount.
///
/// The tapped permanent is carried on the inherited <see cref="ReplacementEvent.AffectedObjects"/>
/// filter, mirroring how <c>TapForManaConditionRule</c> models the identical "you tap a
/// permanent for mana" phrase: <c>CardTypes: ["permanent"]</c> with the tapping player folded
/// into the filter's <c>Controller: You</c> ("you tap" ⇒ a permanent you control produces the
/// mana). A marker event otherwise (no own fields): the multiplier lives on the enclosing
/// <see cref="ReplacementEffect.Modifier"/> and applies to whatever mana the permanent produces.
/// </summary>
[OracleReplacementEvent("manaProduction")]
public sealed record ManaProductionEvent : ReplacementEvent
{
}
