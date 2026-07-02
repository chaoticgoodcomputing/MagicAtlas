namespace MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Registers a concrete <see cref="MagicAST.AST.References.HistoryPredicate"/>
/// subtype with the polymorphic converter. The <paramref name="discriminator"/>
/// is emitted as the <c>"predicateType"</c> property in JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class HistoryPredicateKindAttribute(string discriminator) : PolymorphicTypeAttribute(discriminator) { }
