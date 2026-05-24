namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.Effects.Effect"/> subtype with the
/// polymorphic converter. The <paramref name="discriminator"/> is emitted as the
/// <c>"effectType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OracleEffectAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
