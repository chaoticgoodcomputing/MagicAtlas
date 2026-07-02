namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When you next cast an instant or sorcery spell this turn, copy that spell. You may
/// choose new targets for the copy." — a spell whose resolution creates a delayed
/// triggered ability (CR 603.7) bounded to this turn: the trigger is the next instant
/// or sorcery spell cast by you, and the effect copies that spell with optional
/// retargeting (Doublecast).
///
/// <para>
/// CR 603.7: "An effect may create a delayed triggered ability that can do something
/// at a later time. A delayed triggered ability will contain 'when,' 'whenever,' or
/// 'at,' although that word won't usually begin the ability." CR 603.2: "Whenever a
/// game event or game state matches a triggered ability's trigger event, that ability
/// automatically triggers." "Next" is the once-form of an event trigger — captured by
/// <see cref="TriggerTiming.When"/> ("triggers once") composed with the this-turn
/// <see cref="Duration"/> window, not a separate field; the codebase's
/// <c>GameTime{When:"Next"}</c> marker is reachable only on clock triggers
/// (Timing "At", Event = a GameTime), not an event trigger like <see
/// cref="TriggerEvent.SpellCast"/>.
/// </para>
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to put a
/// copy of it onto the stack ... A copy of a spell or ability copies both the
/// characteristics of the spell or ability and all decisions made for it, including
/// modes, targets ... It does not necessarily have the same target, but only because
/// [an effect] allows choosing of new targets." — the CR basis for <see
/// cref="CopyEffect.MayChooseNewTargets"/>: a copy inherits the original's targets
/// unless the effect grants reselection, which this card does.
/// </para>
/// </summary>
[SpellRule(Priority = 75)]
public sealed class NextCastCopySpellDelayedRule : ISpellRule
{
  // "When you next cast an instant or sorcery spell this turn, copy that spell[. You
  // may choose new targets for the copy]". The trailing terminal period is stripped by
  // the dispatcher; the inter-sentence period before "You may choose new targets" is
  // preserved so the retarget permission rides the copy rather than splitting off as a
  // stray effect (same technique as CopyTargetSpellRule).
  private static readonly Regex _pattern = new(
    @"^When\s+you\s+next\s+cast\s+an\s+instant\s+or\s+sorcery\s+spell\s+this\s+turn,\s+copy\s+that\s+spell"
      + @"(?:\.\s+(?<newtargets>you\s+may\s+choose\s+new\s+targets\s+for\s+the\s+copy))?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.When,
          Event = TriggerEvent.SpellCast,
          Filter = new ObjectFilter
          {
            CardTypes = ["spell", "instant", "sorcery"],
            Controller = ControllerFilter.You,
          },
        },
        Window = UntilTimeDuration.EndOfTurn,
        Effects =
        [
          new CopyEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.It },
            MayChooseNewTargets = m.Groups["newtargets"].Success ? true : null,
          },
        ],
      },
    };
    return true;
  }
}
