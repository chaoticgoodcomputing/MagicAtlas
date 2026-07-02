namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.Abilities.Ability"/> subtype with
/// the polymorphic converter. The <paramref name="discriminator"/> is emitted as
/// the <c>"kind"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OracleAbilityAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
