namespace MagicAST.AST.Effects.Modification;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[Subject] has all [abilityKind] abilities of [controlled creatures]."
///
/// <para>
/// Models the Marvin, Murderous Mimic static ability pattern: "Marvin has all
/// activated abilities of creatures you control that don't have the same name as
/// this creature." The subject (<see cref="Subject"/>) continuously gains every
/// ability of the stated kind from all permanents matching
/// <see cref="SourceFilter"/> while this permanent is on the battlefield.
/// </para>
///
/// <para>
/// <b>Reference, not resolution (ADR 0004):</b> <see cref="SourceFilter"/> encodes
/// the oracle-stated selection criterion ("creatures you control that don't have the
/// same name as this creature") declaratively; the engine evaluates the reference at
/// each relevant game moment to determine which permanents currently satisfy it.
/// </para>
///
/// <para>
/// <b>CR 613.1f</b> (layer 6 — ability-adding continuous effects): this static
/// ability continuously grants the listed ability kind to the subject permanent
/// while the source permanent remains on the battlefield.
/// <br/>
/// <b>CR 602.1</b>: "An activated ability is the only kind of ability that can be
/// activated." (Defines what "activated abilities" means as a class.)
/// </para>
/// </summary>
[OracleEffect("hasAllAbilitiesOfControlledCreatures")]
public sealed record HasAllAbilitiesOfControlledCreaturesEffect : ContinuousEffect
{
  /// <summary>
  /// The permanent that gains the abilities. Typically
  /// <see cref="ObjectReference.Self"/> — the card bearing this static ability
  /// (e.g., Marvin itself).
  /// </summary>
  public required ObjectReference Subject { get; init; }

  /// <summary>
  /// The kind of abilities being continuously granted — "activated" for Marvin.
  /// Descriptive string matching the oracle text ("activated", "triggered", "static").
  /// </summary>
  public required string AbilityKind { get; init; }

  /// <summary>
  /// Filter identifying which permanents on the battlefield supply the abilities.
  /// For Marvin: CardTypes=["creature"], Controller=You, plus an
  /// <see cref="ObjectFilter.Characteristics"/> OtherCharacteristic residual for
  /// "that don't have the same name as this creature" (a relational name predicate
  /// not yet structured as a first-class field — ADR 0001 typed residual).
  /// Per ADR 0004 "reference, not resolution": the filter encodes the oracle-stated
  /// criterion; the engine evaluates which permanents currently satisfy it.
  /// </summary>
  public required ObjectFilter SourceFilter { get; init; }
}
