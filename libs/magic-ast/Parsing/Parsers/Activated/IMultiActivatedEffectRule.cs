namespace MagicAST.Parsing.Parsers.Activated;

using MagicAST.AST.Effects;

/// <summary>
/// Extension of <see cref="IActivatedEffectRule"/> for activated-ability effect
/// shapes that expand to a flat list of sibling effects on
/// <see cref="MagicAST.AST.Abilities.ActivatedAbility.Effects"/> rather than a single
/// <see cref="Effect"/>. Implement this alongside <see cref="IActivatedEffectRule"/>
/// (returning <c>null</c> from <see cref="IActivatedEffectRule.TryMatch"/> so the
/// single-effect path never fires) when a single oracle sentence joins two effects
/// with ", then" and should be represented as a flat array — e.g. Sensei's Divining
/// Top's "Draw a card, then put this artifact on top of its owner's library."
///
/// <para>
/// This mirrors <see cref="MagicAST.Parsing.Parsers.Spell.IMultiSpellRule"/>: not
/// every ", then" is a join (some are a single effect's disposition, such as "look
/// at the top three cards, then put them back in any order"), so the decision belongs
/// to the rule that recognizes the whole sentence, not a generic dispatcher split.
/// </para>
///
/// <para>
/// Discovery: <see cref="MagicAST.Parsing.Parsers.ActivatedAbilityParser"/> picks
/// this up automatically for any <see cref="ActivatedEffectRuleAttribute"/>-decorated
/// class that also implements this interface, trying the multi-effect path before the
/// single-effect path.
/// </para>
/// </summary>
public interface IMultiActivatedEffectRule
{
  /// <summary>
  /// Attempts to match <paramref name="effectText"/> and expand it to a flat effect
  /// list. Returns <c>true</c> and populates <paramref name="effects"/> on success;
  /// returns <c>false</c> and leaves <paramref name="effects"/> null otherwise.
  /// </summary>
  bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects);
}
