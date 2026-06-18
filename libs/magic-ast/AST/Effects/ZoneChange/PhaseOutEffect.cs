namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Target creature [you don't control] phases out." — causes a permanent to phase out
/// until its controller's next untap step.
///
/// <para>
/// CR 702.26 (verbatim): "Phasing is a static ability that modifies the rules of the untap
/// step. … If a permanent phases out, its status changes to 'phased out.' Except for rules
/// and effects that specifically mention phased-out permanents, a phased-out permanent is
/// treated as though it does not exist."
/// </para>
///
/// <para>
/// The parenthetical reminder on Teferi, Master of Time's −3: "(Treat it and anything
/// attached to it as though they don't exist until its controller's next turn.)" is a
/// reminder-text gloss of CR 702.26b; it is stripped from the effect text before parsing.
/// The rule cited is 702.26 (Phasing) for the "phases out" action.
/// </para>
///
/// <para>
/// MAST records the target reference; the phase-in timing (controller's next untap step,
/// CR 702.26a) is engine territory per the descriptive-not-engine doctrine (ADR 0001).
/// </para>
/// </summary>
[OracleEffect("phaseOut")]
public sealed record PhaseOutEffect : Effect
{
  /// <summary>
  /// The permanent that phases out. Typically a targeted creature
  /// ("target creature you don't control") for Teferi, Master of Time's −3.
  /// </summary>
  public required ObjectReference Target { get; init; }
}
