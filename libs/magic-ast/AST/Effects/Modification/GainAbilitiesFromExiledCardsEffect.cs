namespace MagicAST.AST.Effects.Modification;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[Permanents] have all [abilityKind] abilities of all [card filter] exiled with [source]."
///
/// <para>
/// Models the Hazel's Brewmaster static ability pattern: "Foods you control have all
/// activated abilities of all creature cards exiled with this creature."
/// The target permanents (<see cref="Target"/>) continuously gain every ability of the
/// stated kind from the full set of cards currently in exile linked to this object.
/// </para>
///
/// <para>
/// <b>Reference, not resolution (ADR 0004):</b> <see cref="SourceFilter"/> names
/// the class of exiled cards declaratively ("creature cards exiled with this creature")
/// via <see cref="ObjectFilter.ExiledWith"/> pointing at <see cref="ObjectReference.Self"/>;
/// it does not pre-resolve which cards are currently in that set. The engine evaluates
/// the reference at each relevant game moment.
/// </para>
///
/// <para>
/// CR 613.1f (layer 6 — ability-granting continuous effects): the continuous static ability
/// grants the listed ability kinds to the target permanents for as long as the source
/// remains on the battlefield.
/// CR 602.1c: "An activated ability is the only kind of ability that can be activated."
/// </para>
/// </summary>
[OracleEffect("gainAbilitiesFromExiledCards")]
public sealed record GainAbilitiesFromExiledCardsEffect : ContinuousEffect
{
  /// <summary>
  /// The permanents that gain the abilities.
  /// e.g. "Foods you control" → Kind=Each, Filter={Subtypes=["Food"], Controller=You}.
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The kind of abilities being transferred — "activated" for Hazel's Brewmaster.
  /// Descriptive string matching the oracle text ("activated", "triggered", "static").
  /// </summary>
  public required string AbilityKind { get; init; }

  /// <summary>
  /// Filter identifying which exiled cards supply the abilities — "creature cards exiled
  /// with this creature" → CardTypes=["creature"], Zone=Exile, ExiledWith={Kind:Self}.
  /// Per ADR 0004 "reference, not resolution": the filter encodes the oracle-stated
  /// selection criterion; the engine resolves which cards currently satisfy it.
  /// </summary>
  public required ObjectFilter SourceFilter { get; init; }
}
