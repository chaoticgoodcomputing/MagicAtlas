namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.Effects.Replacement.ReplacementEvent"/>
/// subtype with the polymorphic converter. The <paramref name="discriminator"/> is
/// emitted as the <c>"eventType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OracleReplacementEventAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
