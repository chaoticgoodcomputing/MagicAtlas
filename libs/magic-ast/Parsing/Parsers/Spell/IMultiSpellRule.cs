namespace MagicAST.Parsing.Parsers.Spell;

using MagicAST.AST.Effects;

/// <summary>
/// Extension of <see cref="ISpellRule"/> for shapes that expand to a flat list of
/// effects on <see cref="MagicAST.AST.Abilities.SpellAbility.Effects"/> rather than
/// a single <see cref="Effect"/>. Implement this alongside <see cref="ISpellRule"/>
/// (returning <c>false</c> from <see cref="ISpellRule.TryMatch"/> so the single-effect
/// path never fires) when the oracle-text shape naturally produces multiple sibling
/// effects that should be represented as a flat array, not a <c>CompositeEffect</c>.
///
/// <para>
/// Discovery: <see cref="SpellAbilityParser"/> picks this up automatically for any
/// <see cref="SpellRuleAttribute"/>-decorated class that also implements this interface.
/// </para>
/// </summary>
public interface IMultiSpellRule
{
  /// <summary>
  /// Attempts to match <paramref name="text"/> (already trimmed of leading/trailing
  /// whitespace and the trailing period) and expand it to a flat effect list.
  /// Returns <c>true</c> and populates <paramref name="effects"/> on success; returns
  /// <c>false</c> and leaves <paramref name="effects"/> null otherwise.
  /// </summary>
  bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects);
}
