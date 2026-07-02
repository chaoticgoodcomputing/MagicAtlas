namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.Effects.Duration"/> subtype with
/// the polymorphic converter. The <paramref name="discriminator"/> is emitted as
/// the <c>"durationType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OracleDurationAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
