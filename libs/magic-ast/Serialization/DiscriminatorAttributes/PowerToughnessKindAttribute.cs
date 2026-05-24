namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.PowerToughnessValue"/> subtype with the
/// polymorphic converter. The <paramref name="discriminator"/> is emitted as the
/// <c>"valueType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PowerToughnessKindAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
