namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.Effects.TokenCopy.CopyModification"/>
/// subtype with the polymorphic converter. The <paramref name="discriminator"/> is
/// emitted as the <c>"modificationType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CopyModificationKindAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
