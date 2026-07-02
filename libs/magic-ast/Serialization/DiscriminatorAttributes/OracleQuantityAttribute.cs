namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.Quantities.Quantity"/> subtype with
/// the polymorphic converter. The <paramref name="discriminator"/> is emitted as
/// the <c>"quantityType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OracleQuantityAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
