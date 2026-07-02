namespace MagicAST.Serialization;

/// <summary>
/// Marks an abstract record/class as the root of a polymorphic AST hierarchy
/// serialized via <see cref="PolymorphicReflectionConverter{TBase}"/>.
///
/// The named discriminator property is injected by the converter on write and
/// consumed (then stripped) by the converter on read. The discriminator value
/// itself is supplied per-derived-type by a <see cref="PolymorphicTypeAttribute"/>
/// (or one of its subclasses such as <see cref="DiscriminatorAttributes.OracleAbilityAttribute"/>).
/// </summary>
/// <param name="discriminatorPropertyName">
/// The JSON property name carrying the type discriminator (e.g., <c>"kind"</c>,
/// <c>"effectType"</c>). Must match what the on-disk fixtures expect — changing
/// it is a breaking change for any persisted JSON.
/// </param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PolymorphicBaseAttribute(string discriminatorPropertyName) : Attribute
{
  /// <summary>The JSON property name carrying the type discriminator.</summary>
  public string DiscriminatorPropertyName { get; } = discriminatorPropertyName;
}
