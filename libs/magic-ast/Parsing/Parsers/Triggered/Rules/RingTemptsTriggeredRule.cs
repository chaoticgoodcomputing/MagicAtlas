namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "the Ring tempts you." — the Rule 701.54 keyword action on the triggered
/// side. Matches the bare keyword action when it is the entire effect body of
/// a triggered ability (e.g. Uruk-hai Berserker's ETB trigger).
///
/// CR 701.54a: "Certain spells and abilities have the text 'the Ring tempts
/// you.' Each time the Ring tempts you, choose a creature you control. That
/// creature becomes your Ring-bearer until another creature becomes your
/// Ring-bearer or another player gains control of it."
///
/// REUSE-ONLY emits the existing <see cref="RingTemptsEffect"/> node.
/// </summary>
[TriggeredRule]
public sealed class RingTemptsTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!trimmed.Equals("the Ring tempts you", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    effect = EffectWrap.Optional(new RingTemptsEffect(), false);
    return true;
  }
}
