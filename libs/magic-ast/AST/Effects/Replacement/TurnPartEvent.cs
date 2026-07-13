namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A turn step, phase, or whole turn as a replaceable event — the typed target of
/// a "Skip [step/phase/turn]" replacement effect. CR 614.10 (verbatim): "An effect
/// that causes a player to skip an event, step, phase, or turn is a replacement
/// effect. \"Skip [something]\" is the same as \"Instead of doing [something], do
/// nothing.\" Once a step, phase, or turn…" — so "Skip your draw step" is modeled
/// as a <see cref="ReplacementEffect"/> whose <see cref="ReplacementEffect.Event"/>
/// is this node (<see cref="Part"/> = <see cref="TurnPart.Draw"/>), with
/// <c>OriginalEventOccurs = false</c> and no <c>Replacement</c> ("do nothing").
/// </summary>
/// <remarks>
/// Keeps the skipped step TYPED rather than free text: <see cref="Part"/> reuses the
/// shared <see cref="TurnPart"/> turn-structure vocabulary (CR 500-series), so the
/// same node models "Skip your draw step" (Draw), "Skip your combat phase" (Combat),
/// "Skip your next turn" (Turn), etc. <see cref="Whose"/> records the "your" qualifier
/// (CR 500.10 turn-structure context — whose step/phase/turn is skipped); null when
/// the oracle text leaves it unqualified. MAST describes the printed replacement; the
/// engine applies the skip against the actual turn structure (reference-not-resolution,
/// ADR 0004).
/// </remarks>
[OracleReplacementEvent("turnPart")]
public sealed record TurnPartEvent : ReplacementEvent
{
  /// <summary>
  /// The step, phase, or whole turn being skipped — e.g. <see cref="TurnPart.Draw"/>
  /// for "your draw step".
  /// </summary>
  public required TurnPart Part { get; init; }

  /// <summary>
  /// Whose step/phase/turn — "your draw step" => <see cref="ControllerFilter.You"/>.
  /// Null when the oracle text leaves it unqualified.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? Whose { get; init; }
}
