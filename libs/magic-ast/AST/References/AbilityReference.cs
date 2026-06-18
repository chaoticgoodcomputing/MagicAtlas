namespace MagicAST.AST.References;

using System.Text.Json.Serialization;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A filter over a class of abilities or spells, keyed on the surviving keyword
/// identity — the <b>reference half</b> of the production/reference duality
/// (ADR 0003). When a keyword decomposes into shared primitives, its identity
/// survives as a typed <see cref="KeywordAbility"/> label; an
/// <c>AbilityReference</c> is what another card's effect matches that label on.
///
/// <para>
/// Strong Back's "Equip abilities you activate that target enchanted creature"
/// and "Aura spells you cast that target enchanted creature" are both
/// <c>AbilityReference</c>s with a controller and an optional target predicate.
/// Per ADR 0003, the identity that survives decomposition must be the same
/// identity a filter matches on — which is why this is keyed on the typed
/// <see cref="KeywordAbility"/> enum rather than a bare string.
/// </para>
/// </summary>
[PolymorphicBase("Kind")]
[JsonConverter(typeof(PolymorphicReflectionConverter<AbilityReference>))]
public abstract record AbilityReference
{
  /// <summary>
  /// Whose abilities/spells the reference matches — "you activate" / "you cast"
  /// (CR 109.5 controller). Optional: an unqualified reference matches any
  /// controller's.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? Controller { get; init; }

  /// <summary>
  /// Restricts the reference to abilities/spells that <i>target</i> a particular
  /// object — Strong Back's "that target enchanted creature". Null when the
  /// reference does not constrain the target (LeoninShikari's "equip abilities").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? TargetsObject { get; init; }
}

/// <summary>
/// Matches a class of <b>activated</b> abilities by their surviving keyword
/// identity — Strong Back / LeoninShikari "Equip abilities" (CR 702.6a, the
/// Equip activated ability being filtered).
/// </summary>
[AbilityReferenceKind("activatedAbility")]
public sealed record ActivatedAbilityReference : AbilityReference
{
  /// <summary>The keyword identity the matched activated ability carries.</summary>
  public required KeywordAbility Keyword { get; init; }
}

/// <summary>
/// Matches a class of <b>spells</b> by a characteristic filter — Strong Back's
/// "Aura spells you cast". The spell's qualifying characteristics live in
/// <see cref="Filter"/> (subtype Aura, controller You).
/// </summary>
[AbilityReferenceKind("spell")]
public sealed record SpellReference : AbilityReference
{
  /// <summary>The characteristics a matched spell must have (e.g. subtype Aura).</summary>
  public required ObjectFilter Filter { get; init; }
}

/// <summary>
/// Matches <b>activated abilities</b> that belong to a class of permanents
/// identified by <see cref="PermanentFilter"/> — Forensic Gadgeteer's
/// "Activated abilities of artifacts you control" (CR 602.1c). Unlike
/// <see cref="ActivatedAbilityReference"/> (which keys on a keyword identity),
/// this variant keys on the <i>permanent's</i> card-type characteristics, so
/// it clusters cards that reduce activation costs for a whole category of
/// permanents (Vedalken Orrery-style effects, rock-reduction effects).
///
/// <para>
/// The <c>Controller</c> base field carries "you control" when present.
/// </para>
/// </summary>
[AbilityReferenceKind("objectActivatedAbility")]
public sealed record ObjectActivatedAbilityReference : AbilityReference
{
  /// <summary>
  /// The filter that the <em>permanent</em> owning the activated ability must satisfy
  /// (e.g. <c>CardTypes: ["artifact"], Controller: You</c> for "artifacts you control").
  /// </summary>
  public required ObjectFilter PermanentFilter { get; init; }
}
