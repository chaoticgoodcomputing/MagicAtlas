namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.References.AbilityReference"/>
/// subtype with the polymorphic converter. The <paramref name="discriminator"/>
/// is emitted as the <c>"Kind"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AbilityReferenceKindAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
