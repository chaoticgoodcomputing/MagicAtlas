namespace MagicAST.Parsing.Parsers.Spell;

using MagicAST.AST.Effects;

/// <summary>
/// One spell-effect recognition rule. Each implementation maps a single oracle-text
/// shape to its <see cref="Effect"/> AST node. Rules are discovered by reflection at
/// <see cref="SpellAbilityParser"/> construction via the
/// <see cref="SpellRuleAttribute"/> decoration and dispatched in descending
/// <see cref="SpellRuleAttribute.Priority"/> order — first match wins.
/// </summary>
public interface ISpellRule
{
  /// <summary>
  /// Attempts to match <paramref name="text"/> (already trimmed of leading/trailing
  /// whitespace and the trailing period). Returns <c>true</c> and populates
  /// <paramref name="effect"/> on a successful match; returns <c>false</c> and
  /// leaves <paramref name="effect"/> null otherwise.
  /// </summary>
  bool TryMatch(string text, out Effect? effect);
}
