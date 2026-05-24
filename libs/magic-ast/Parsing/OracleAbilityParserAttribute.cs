namespace MagicAST.Parsing;

using MagicAST.AST.Abilities;

/// <summary>
/// Marks a class as the parser for a specific <see cref="AbilityKind"/>.
/// Discovered at startup by <see cref="AbilityParserRegistry"/>.
///
/// The tagged class must implement <see cref="IAbilityParser"/> and have a
/// public parameterless constructor.
/// </summary>
/// <param name="kind">The ability kind this parser handles.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OracleAbilityParserAttribute(AbilityKind kind) : Attribute
{
  /// <summary>The ability kind this parser is registered for.</summary>
  public AbilityKind Kind { get; } = kind;
}
