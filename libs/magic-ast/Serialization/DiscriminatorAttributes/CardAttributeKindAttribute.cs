namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.CardAttribute"/> subtype with the
/// polymorphic converter. The <paramref name="discriminator"/> is emitted as the
/// <c>"kind"</c> property in JSON. Named <c>CardAttributeKind</c> rather than
/// <c>OracleCardAttribute</c> to avoid the awkward <c>Attribute</c>-suffix
/// collision with the <c>CardAttribute</c> base record.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CardAttributeKindAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
