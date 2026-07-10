namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "The next time you cast an instant or sorcery spell from your hand this turn,
/// put that card into your hand instead of into your graveyard as it resolves." —
/// Soulfire Grand Master's activated-ability effect.
///
/// <para>
/// This is the once-form "next … this turn" delayed shield (CR 603.7: "An effect
/// may create a delayed triggered ability that can do something at a later time. A
/// delayed triggered ability will contain 'when,' 'whenever,' or 'at,' although that
/// word won't usually begin the ability.") over the buyback-style zone-change
/// replacement of CR 702.27a ("...put this spell into its owner's hand instead of
/// into that player's graveyard as it resolves."). Mirrors
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.NextCastCopySpellDelayedRule"/>
/// (Doublecast's "When you next cast an instant or sorcery spell this turn, copy
/// that spell.") exactly for the trigger half — same <see cref="TriggerEvent.SpellCast"/>
/// event, same <c>CardTypes = ["spell", "instant", "sorcery"]</c> type-disjunction
/// filter, same <see cref="TriggerTiming.When"/> ("next" is the once-form of an event
/// trigger, composed with the this-turn <c>Window</c> rather than a separate field),
/// same <see cref="UntilTimeDuration.EndOfTurn"/> window — but reached via the
/// ACTIVATED-ability registry (this ability has a mana cost, not a spell body) and
/// carrying an added "from your hand" qualifier on the filter (<c>Zone = Zone.Hand</c>
/// — per <see cref="MagicAST.AST.References.ObjectFilter.ExcludedZone"/>'s documented
/// convention, a stated zone other than <see cref="Zone.Stack"/> on a
/// <c>CardTypes=["spell", ...]</c> filter is shorthand for the pre-cast origin zone,
/// so a POSITIVE <c>Zone = Zone.Hand</c> names "cast from your hand").
/// </para>
///
/// <para>
/// The resolution effect is the SAME shape <see cref="BuybackStaticRule"/>'s Ability 2
/// already emits for CR 702.27a's identical "put this spell into its owner's hand
/// instead of into that player's graveyard as it resolves" clause — a
/// <see cref="ReplacementEffect"/> over a <see cref="ZoneChangeEvent"/> (origin stack,
/// destination graveyard) with <c>OriginalEventOccurs = false</c> ("instead") and a
/// <see cref="ReturnToHandEffect"/> as the replacement action — except the affected
/// object here is not "this spell" (<c>Self</c>) but the delayed trigger's anaphoric
/// "that card", i.e. the spell that just matched the trigger filter, so the
/// <see cref="ReturnToHandEffect.Target"/> is <see cref="ObjectReferenceKind.It"/>
/// (CR 109.2) rather than <c>Self</c>. Timing ("as it resolves") is the natural clock
/// of the zone-change event, not baked into the effect discriminator.
/// </para>
///
/// <para>
/// Anchored (^…$) to the exact surface, so it cannot collide with any sibling
/// activated-effect rule. Priority 80: above the general-purpose activated rule band
/// (50) — no other activated rule shares this "the next time you cast … put that
/// card" opening.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 80)]
public sealed class NextInstantSorceryCastFromHandReturnInsteadEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^The\s+next\s+time\s+you\s+cast\s+an\s+instant\s+or\s+sorcery\s+spell\s+from\s+your\s+hand\s+this\s+turn,\s+" +
      @"put\s+that\s+card\s+into\s+your\s+hand\s+instead\s+of\s+into\s+your\s+graveyard\s+as\s+it\s+resolves\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new CreateDelayedTriggerEffect
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
            Zone = Zone.Hand,
          },
        },
        Window = UntilTimeDuration.EndOfTurn,
        Effects =
        [
          new ReplacementEffect
          {
            Event = new ZoneChangeEvent
            {
              OriginZone = Zone.Stack,
              DestinationZone = Zone.Graveyard,
            },
            OriginalEventOccurs = false,
            Replacement = new ReturnToHandEffect
            {
              Target = new ObjectReference { Kind = ObjectReferenceKind.It },
            },
          },
        ],
      },
    };
  }
}
