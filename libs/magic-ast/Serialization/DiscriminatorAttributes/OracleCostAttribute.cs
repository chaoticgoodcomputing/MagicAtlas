namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.Costs.Cost"/> subtype with the
/// polymorphic converter. The <paramref name="discriminator"/> is emitted as the
/// <c>"costType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OracleCostAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
