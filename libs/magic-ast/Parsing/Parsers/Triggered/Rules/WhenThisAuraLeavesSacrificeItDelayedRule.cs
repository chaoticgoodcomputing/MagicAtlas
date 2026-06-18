namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Recognises the "When this Aura leaves the battlefield, that creature's controller
/// sacrifices it." delayed-trigger sentence produced by the Animate Dead / Dance of
/// the Dead family.
///
/// <para>
/// This sentence appears as the second sentence in the complex ETB ability's effect
/// body, AFTER the composite lose-gain-return-attach sentence. It creates a
/// <see cref="CreateDelayedTriggerEffect"/> whose <see cref="DelayedTriggeredAbility"/>
/// fires when this Aura leaves the battlefield (CR 603.7 — a delayed triggered
/// ability created by a resolving effect).
/// </para>
///
/// <para>
/// The sacrifice target is the enchanted creature ("it" — the anaphoric reference
/// to the creature previously returned to the battlefield by the preceding effects).
/// "That creature's controller sacrifices it" — the sacrificer is that creature's
/// controller, not necessarily the Aura's controller. MAST records the sacrifice
/// effect descriptively via <see cref="SacrificeEffect"/> with
/// <see cref="ObjectReferenceKind.It"/>; the "controller" subject is engine-derived
/// from the triggered ability's implicit actor (CR 603.3 — a triggered ability's
/// controller is the player who controlled the ability's source when it triggered,
/// but here oracle specifies the enchanted creature's controller — the engine
/// resolves this per CR 303.4e). Descriptive, not engine-executable.
/// </para>
///
/// <para>
/// CR 702.5a (Enchant); CR 303.4 (Aura); CR 603.7 (delayed triggered abilities);
/// CR 701.21 (Sacrifice).
/// </para>
///
/// <para>
/// This rule is tightly anchored (^...$) and keyed on the conjunction of "this Aura
/// leaves the battlefield" + "controller sacrifices it" so it cannot match as a
/// substring of a broader trigger or effect. Priority 95 ensures it fires before the
/// generic <see cref="SacrificeTriggeredRule"/>.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class WhenThisAuraLeavesSacrificeItDelayedRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^When\s+this\s+Aura\s+leaves\s+the\s+battlefield\s*,\s*that\s+creature'?s\s+controller\s+sacrifices\s+it\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    // CR 603.7 — a delayed triggered ability created by a resolving effect.
    // The delayed trigger fires when this Aura (self-reference, IsSelf) leaves
    // the battlefield, then the sacrifice effect resolves.
    effect = new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.When,
          Event = TriggerEvent.LeavesTheBattlefield,
          Filter = new ObjectFilter
          {
            CardTypes = ["aura"],
            IsSelf = true,
          },
        },
        Effects =
        [
          new SacrificeEffect { Target = ObjectReference.It() },
        ],
      },
    };
    return true;
  }
}
