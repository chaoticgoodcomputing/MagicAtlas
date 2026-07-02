namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Recognises the delayed-sacrifice sentence:
/// <list type="bullet">
///   <item>"Sacrifice it at the beginning of the next end step."</item>
///   <item>"Sacrifice it at end of turn."</item>
/// </list>
///
/// This sentence appears as a trailing clause in multi-sentence spell abilities
/// where a creature or token was obtained or created earlier in the same ability
/// text — for example: "Gain control of target creature until end of turn. Untap
/// that creature. It gains haste until end of turn. Sacrifice it at the beginning
/// of the next end step." (Act of Treason family). The pronoun "it" refers to the
/// object described in the preceding clause (Rule 109.2 — game objects).
///
/// MAST records this as a <see cref="SacrificeEffect"/> whose
/// <see cref="SacrificeEffect.Target"/> is <see cref="ObjectReferenceKind.It"/>
/// and whose <see cref="SacrificeEffect.Duration"/> is
/// <see cref="AtBeginningOfNextEndStepDuration"/>. This is descriptive
/// (Rule 701.21 — Sacrifice), not an engine action; the timing semantics are
/// carried by the duration node per the descriptive-not-engine doctrine.
///
/// Priority 70: must supersede generic sacrifice rules that match "sacrifice [it]"
/// without the duration clause.
///
/// Rule citations: 701.21 (Sacrifice), 513 (End Step), 603.7 (delayed triggers).
/// </summary>
[SpellRule(Priority = 70)]
public sealed class SacrificeAtEndStepSpellRule : ISpellRule
{
  // Matches both the "beginning of the next end step" and "end of turn" variants.
  // The pronoun may be "it", "this creature", or "this permanent".
  private static readonly Regex _pattern = new(
    @"^Sacrifice\s+(it|this\s+creature|this\s+permanent)\s+at\s+the\s+beginning\s+of\s+the\s+next\s+end\s+step$",
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

    effect = new MagicAST.AST.Effects.Core.CreateDelayedTriggerEffect
    {
      DelayedTrigger = new MagicAST.AST.Abilities.DelayedTriggeredAbility
      {
        Trigger = new MagicAST.AST.Triggers.TriggerCondition
        {
          Timing = MagicAST.AST.Triggers.TriggerTiming.At,
          Event = new MagicAST.AST.References.GameTime
          {
            Part = MagicAST.AST.References.TurnPart.End,
            Edge = MagicAST.AST.References.TimeBoundary.Beginning,
            When = MagicAST.AST.References.TimeRelation.Next,
          },
        },
        Effects = [new SacrificeEffect { Target = ObjectReference.It() }],
      },
    };
    return true;
  }
}
