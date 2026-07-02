namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.Abilities.Condition"/> subtype
/// with the polymorphic converter. The <paramref name="discriminator"/> is
/// emitted as the <c>"ConditionType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ConditionKindAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
