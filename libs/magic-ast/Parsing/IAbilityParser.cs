namespace MagicAST.Parsing;

using MagicAST.AST.Abilities;

/// <summary>
/// Contract for an ability-kind-specific parser. Each implementation handles
/// one <see cref="AbilityKind"/> and is discovered at startup by
/// <see cref="AbilityParserRegistry"/> via an
/// <see cref="OracleAbilityParserAttribute"/> decoration.
///
/// Implementations own their own fallback behavior — <see cref="Parse"/> must
/// always return at least one ability node (typically an
/// <see cref="UnparsedAbility"/> on failure) so the orchestrator never needs
/// to special-case missing output.
/// </summary>
public interface IAbilityParser
{
  /// <summary>
  /// Parses the clause into one or more abilities. Some clauses (like
  /// comma-separated keywords) expand into multiple abilities.
  /// </summary>
  /// <remarks>
  /// Must never return an empty list. On parse failure, return a list
  /// containing exactly one <see cref="UnparsedAbility"/>.
  /// </remarks>
  IReadOnlyList<Ability> Parse(
    OracleClause clause,
    ClauseClassification classification
  );
}
