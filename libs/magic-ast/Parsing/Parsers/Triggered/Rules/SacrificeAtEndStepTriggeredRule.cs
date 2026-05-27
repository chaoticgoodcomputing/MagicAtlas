namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Recognises the delayed-sacrifice sentence within a triggered ability effect body:
/// <list type="bullet">
///   <item>"Sacrifice it at the beginning of the next end step."</item>
/// </list>
///
/// This sentence appears as a trailing clause in multi-sentence triggered ability
/// effects — typically after a token-creation effect. For example:
/// "Whenever this creature attacks, create a 1/1 red Goblin creature token.
/// Sacrifice it at the beginning of the next end step." The pronoun "it" refers
/// to the token or creature created/obtained by the preceding clause
/// (Rule 109.2 — game objects).
///
/// MAST records this as a <see cref="SacrificeEffect"/> whose
/// <see cref="SacrificeEffect.Target"/> is <see cref="ObjectReferenceKind.It"/>
/// and whose <see cref="SacrificeEffect.Duration"/> is
/// <see cref="AtBeginningOfNextEndStepDuration"/>. Descriptive, not engine-executable
/// (descriptive-not-engine doctrine). The trigger timing semantics are carried
/// by the duration node.
///
/// Priority 70: must supersede <see cref="SacrificeTriggeredRule"/> (plain "sacrifice it")
/// so the more-specific duration-bearing form fires first.
///
/// Rule citations: 701.21 (Sacrifice), 513 (End Step), 603.7 (delayed triggers).
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class SacrificeAtEndStepTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+(it|this\s+creature|this\s+permanent)\s+at\s+the\s+beginning\s+of\s+the\s+next\s+end\s+step\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new SacrificeEffect
    {
      Target = ObjectReference.It(),
      Duration = new AtBeginningOfNextEndStepDuration(),
    };
    return true;
  }
}
