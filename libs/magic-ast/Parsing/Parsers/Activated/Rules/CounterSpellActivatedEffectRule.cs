namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;
using MagicAST.Parsing.Parsers.Spell.Rules;

/// <summary>
/// "Counter target [color/type] spell [unless its controller pays {cost}]" reached
/// as the effect interior of an activated ability — e.g. Douse's
/// "{1}{U}: Counter target red spell." (Scourge). CR 701.6a: "To counter a spell or
/// ability means to cancel it, removing it from the stack."
///
/// <para>
/// The counter-spell shape and all of its filter dimensions (color words, non&lt;color&gt;
/// predicates, bare card-type qualifier, "with mana value N", and the optional
/// "unless its controller pays {cost}" tail) are identical whether the effect is the
/// resolution of a spell (instant/sorcery) or of an activated ability. Rather than
/// duplicate that recognizer, this activated-effect rule delegates to the single
/// consolidated <see cref="CounterSpellRule"/> surface — the same anchored
/// (<c>^Counter\s+target … spell …$</c>) matcher used by the spell parser — so both
/// paths stay in lock-step and there is no second regex to drift.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 60)]
public sealed class CounterSpellActivatedEffectRule : IActivatedEffectRule
{
  private readonly CounterSpellRule _counterSpell = new();

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText)
  {
    return _counterSpell.TryMatch(effectText.Trim(), out var effect) ? effect : null;
  }
}
