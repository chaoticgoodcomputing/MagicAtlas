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
}
