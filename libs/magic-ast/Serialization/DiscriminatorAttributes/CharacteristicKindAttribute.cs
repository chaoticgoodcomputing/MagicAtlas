namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.References.Characteristic"/>
/// subtype with the polymorphic converter. The <paramref name="discriminator"/>
/// is emitted as the <c>"CharacteristicType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CharacteristicKindAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
