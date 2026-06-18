namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "At the beginning of the next end step, sacrifice that token unless you pay an amount
/// of {E} equal to its mana value." — Satya, Aetherflux Genius's delayed-trigger
/// energy-buyback pattern (CR 603.7: delayed triggered ability; CR 107.14: energy counters;
/// CR 701.21: sacrifice).
///
/// <para>
/// Oracle structure: this sentence encodes a <see cref="CreateDelayedTriggerEffect"/>
/// that schedules a one-shot trigger at the beginning of the next end step. When that
/// trigger resolves its controller must either pay energy equal to the token's mana value
/// or sacrifice it. Modelled as <see cref="PreventableEffect"/> wrapping
/// <see cref="SacrificeEffect"/> with an <see cref="UnlessClause"/> whose cost is
/// <see cref="PayEnergyCost"/> with a <see cref="DerivedQuantity"/> referencing
/// <see cref="DerivedKind.ManaValue"/> (the mana value of the token = "it").
/// </para>
///
/// <para>
/// "That token" / "it" — both pronouns refer to the token created by the preceding
/// copy-effect sentence in the same triggered ability. MAST models this as
/// <see cref="ObjectReferenceKind.It"/> (the conventional back-reference pronoun,
/// matching <see cref="SacrificeAtEndStepTriggeredRule"/> and
/// <see cref="SacrificeUnlessPayTriggeredRule"/>).
/// </para>
///
/// <para>
/// ANCHORED (^…$): the "at the beginning of the next end step" phrase could match
/// inside a broader sentence on a sibling trigger, so the anchor prevents substring
/// collisions. Priority 73 — above <see cref="SacrificeAtEndStepTriggeredRule"/> (70)
/// so the more-specific energy-buyback form is claimed here first.
/// </para>
///
/// <para>
/// Rule citations: CR 603.7 (delayed triggered ability), CR 107.14 (energy symbol),
/// CR 513 (end step), CR 701.21 (sacrifice), CR 117.7 (unless clause).
/// </para>
/// </summary>
[TriggeredRule(Priority = 73)]
public sealed class SacrificeItAtEndStepUnlessPayEnergyEqualToManaValueRule : ITriggeredRule
{
  // "At the beginning of the next end step, sacrifice that token unless you pay an
  // amount of {E} equal to its mana value."
  // Terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^at\s+the\s+beginning\s+of\s+the\s+next\s+end\s+step,\s+sacrifice\s+(?:it|that\s+token)\s+unless\s+you\s+pay\s+an\s+amount\s+of\s+\{E\}\s+equal\s+to\s+its\s+mana\s+value$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    // CR 603.7: create a delayed triggered ability that fires at the next end step.
    // CR 107.14: {E} = one energy counter; "an amount of {E} equal to its mana value"
    //            = PayEnergyCost with DerivedQuantity { DerivedFrom = ManaValue }.
    // CR 701.21: sacrifice the token unless the energy is paid.
    effect = new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.At,
          Event = new GameTime
          {
            Part = TurnPart.End,
            Edge = TimeBoundary.Beginning,
            When = TimeRelation.Next,
          },
        },
        Effects =
        [
          EffectWrap.Preventable(
            new SacrificeEffect { Target = ObjectReference.It() },
            new UnlessClause
            {
              Player = ObjectReference.You(),
              Cost = new PayEnergyCost
              {
                Amount = new DerivedQuantity { DerivedFrom = DerivedKind.ManaValue },
              },
            }
          ),
        ],
      },
    };
    return true;
  }
}
