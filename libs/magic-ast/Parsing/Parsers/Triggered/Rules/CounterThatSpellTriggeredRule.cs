namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "counter that spell" — a bare counter effect that counters the spell
/// referenced by the trigger's own event (the "that" pronoun anaphoric back-reference
/// to the triggering spell). Encodes as <see cref="CounterSpellEffect"/> with
/// <c>Target = { Kind: It }</c> — the <c>It</c> reference follows the MAST
/// pronoun convention: the object named by the trigger condition (analogous to the
/// Ward-keyword "counter it" shape; WilsonRefinedGrizzly gold fixture).
///
/// CR 701.6a: "To counter a spell or ability means to cancel it, removing it from
/// the stack. It doesn't resolve and none of its effects occur. A countered spell
/// is put into its owner's graveyard."
///
/// This rule is collision-free: anchored (^…$) so it cannot match as a substring
/// of a more-specific sibling (e.g. CounterSpellRule's "counter target spell" or
/// CounterUnlessPaysRule's "counter it unless …"). The surface phrase
/// "counter that spell" occurs only in this bare-counter pattern; the Ward shape
/// uses "counter it" (handled by the Ward combinator, not this rule) and the spell
/// rules use "counter target spell".
///
/// Priority 80: above the general-purpose rules (50) so the bare anaphoric form is
/// claimed before a broader match could fire.
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class CounterThatSpellTriggeredRule : ITriggeredRule
{
  // Anchored full-match: "counter that spell" only (no "unless", no "target").
  private static readonly Regex _pattern = new(
    @"^counter\s+that\s+spell$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    // "that spell" is the anaphoric back-reference to the triggering spell —
    // the same object the trigger condition matched. Encoded as It (pronoun
    // back-reference, mirroring the Ward combinator's "counter it" shape).
    effect = new CounterSpellEffect
    {
      Target = ObjectReference.It(),
    };
    return true;
  }
}
