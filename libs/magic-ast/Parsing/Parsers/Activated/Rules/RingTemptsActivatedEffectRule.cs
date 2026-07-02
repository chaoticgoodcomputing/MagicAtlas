namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "The Ring tempts you." — the Rule 701.54 keyword action as an effect within
/// an activated ability. Emits the existing <see cref="RingTemptsEffect"/> node.
///
/// CR 701.54a (verbatim): "Certain spells and abilities have the text 'the Ring
/// tempts you.' Each time the Ring tempts you, choose a creature you control. That
/// creature becomes your Ring-bearer until another creature becomes your Ring-bearer
/// or another player gains control of it."
///
/// Distinct from <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.RingTemptsTriggeredRule"/>
/// which is used when the Ring tempts you is the entire body of a triggered ability
/// and wraps the effect in an <c>Optional</c>. Here, as an activated-ability effect,
/// "The Ring tempts you" is a mandatory instruction (not optional) — the activation
/// always tempts you when paid.
///
/// Anchored (^…$): collision-free with any sibling that checks for "ring" substring.
/// Priority 80: above general-purpose rules (50) so this exact surface is claimed
/// before broader matchers can fire.
/// </summary>
[ActivatedEffectRule(Priority = 80)]
public sealed class RingTemptsActivatedEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');

    if (!trimmed.Equals("The Ring tempts you", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return new RingTemptsEffect();
  }
}
