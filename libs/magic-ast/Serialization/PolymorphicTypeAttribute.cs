namespace MagicAST.Serialization;

/// <summary>
/// Abstract base for attributes that register a derived concrete type into a
/// polymorphic AST hierarchy. Each polymorphic base has its own concrete subclass
/// (e.g., <see cref="DiscriminatorAttributes.OracleAbilityAttribute"/>,
/// <see cref="DiscriminatorAttributes.OracleEffectAttribute"/>) so that the
/// attribute name signals which hierarchy the type belongs to.
///
/// Discovered at startup by <see cref="PolymorphicReflectionConverter{TBase}"/>.
/// </summary>
/// <param name="discriminator">
/// The discriminator value emitted in JSON for this concrete type
/// (e.g., <c>"triggered"</c>, <c>"dealDamage"</c>).
/// </param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public abstract class PolymorphicTypeAttribute(string discriminator) : Attribute
{
  /// <summary>The discriminator string emitted/expected in JSON.</summary>
  public string Discriminator { get; } = discriminator;

  /// <summary>
  /// Discriminators in the SAME family that this one is a near-duplicate of (Levenshtein ≤ 2, or one is
  /// a prefix-stem of the other), together with <see cref="Reason"/> — the architectural ruling that both
  /// must exist.
  ///
  /// <para>Multi-valued: one discriminator can sit near several others (<c>shuffle</c> is near
  /// <c>shuffleIntoLibrary</c>, <c>shuffleGraveyardIntoLibrary</c> and
  /// <c>shuffleCardsFromGraveyardIntoLibrary</c>), so this is a set, not a single name. A pair is
  /// explained if EITHER side declares the other — the relation is symmetric, as it was when these
  /// rulings lived in <c>discriminator-justifications.json</c>. Convention: the justification is declared
  /// on the <b>more specific</b> member of the pair (the one whose existence beside the shorter name is
  /// what needs explaining), because that is the type whose deletion should retire the ruling.</para>
  ///
  /// <para><b>Why this is an attribute and not a JSON whitelist</b> (ADR-0004 §1, issue #38): liveness
  /// becomes structural. A justification file can outlive its subject — delete the type and a stale
  /// entry survives, describing a discriminator that no longer exists, which is exactly how
  /// <c>discriminator-baseline.json</c> rotted. A declaration-site attribute cannot: the ruling is
  /// deleted by the same edit that deletes the type. The near-duplicate check itself is a Flowthru
  /// REPORT (<c>DiscriminatorGovernance</c>), not a gate — the gate that remains is the HARD per-family
  /// collision check (<c>DiscriminatorUniquenessTests</c>), which needs no whitelist because a genuine
  /// duplicate is always a serialization bug.</para>
  /// </summary>
  public string[] NearDuplicateOf { get; set; } = [];

  /// <summary>The ruling behind <see cref="NearDuplicateOf"/> — why the near-duplicate names are two
  /// real concepts rather than vocabulary sprawl. Cites the CR where the distinction is a rules
  /// distinction. Required whenever <see cref="NearDuplicateOf"/> is non-empty; one reason covers every
  /// counterpart listed there.</summary>
  public string? Reason { get; set; }
}
