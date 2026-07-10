namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Exile it at the beginning of your next end step." — the delayed-exile sentence
/// that trails a token-creation (or other object-obtaining) effect in a multi-sentence
/// spell ability (Stone Idol Trap). The pronoun "it" back-references the object created
/// or obtained by the preceding clause (CR 109.2 — game objects).
///
/// <para>
/// CR 603.7: a triggered ability created by a resolving effect is a delayed triggered
/// ability. MAST records this as a <see cref="CreateDelayedTriggerEffect"/> whose
/// <see cref="DelayedTriggeredAbility.Trigger"/> fires "at the beginning of your next
/// end step" (a <see cref="GameTime"/> clock point with <see cref="GameTime.Whose"/> =
/// <see cref="ControllerFilter.You"/> for the "your" qualifier) and whose resolution
/// exiles <see cref="ObjectReference.It"/> — the delayed-exile sibling of
/// <see cref="SacrificeAtEndStepSpellRule"/> (which handles the unqualified "the next
/// end step" sacrifice sentence). Distinct GameTime.Whose is the only structural
/// difference; the exiled-vs-sacrificed action is <see cref="ExileEffect"/> rather than
/// <c>SacrificeEffect</c>.
/// </para>
///
/// <para>
/// Priority 71 — mirrors <see cref="SacrificeAtEndStepSpellRule"/>'s rationale
/// (must supersede generic exile rules that match "exile [it]" without the duration
/// clause). Fully anchored (^…$).
/// </para>
///
/// <para>Rule citations: 701.13a (Exile); 513 (End Step); 603.7 (delayed triggers).</para>
/// </summary>
[SpellRule(Priority = 71)]
public sealed class ExileAtYourNextEndStepSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Exile\s+(it|this\s+creature|this\s+permanent|this\s+token)\s+at\s+the\s+beginning\s+of\s+your\s+next\s+end\s+step$",
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

    effect = new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new MagicAST.AST.Abilities.DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.At,
          Event = new GameTime
          {
            Part = TurnPart.End,
            Edge = TimeBoundary.Beginning,
            When = TimeRelation.Next,
            Whose = ControllerFilter.You,
          },
        },
        Effects = [new ExileEffect { Target = ObjectReference.It() }],
      },
    };
    return true;
  }
}
