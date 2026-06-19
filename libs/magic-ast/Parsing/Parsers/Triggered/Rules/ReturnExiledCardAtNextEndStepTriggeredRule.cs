namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return that card to the battlefield under its owner's control at the beginning
/// of the next end step." — the delayed-return resolution sentence of a blink that
/// returns NEXT end step (Flickerwisp).
///
/// <para>
/// Unlike an immediate blink (exile + return now, the
/// <see cref="ExileThenReturnFlickerTriggeredRule"/> shape), this clause sets up a
/// <i>delayed</i> triggered ability (CR 603.7): "at the beginning of the next end
/// step" is the firing point of an ability created when the ETB ability resolves,
/// not a duration on the return effect itself (ADR 0002/0004). The return therefore
/// lives INSIDE a <see cref="CreateDelayedTriggerEffect"/> whose
/// <see cref="MagicAST.AST.Abilities.DelayedTriggeredAbility"/> fires
/// "At" the <see cref="MagicAST.AST.References.TurnPart.End"/> step's
/// <see cref="MagicAST.AST.References.TimeBoundary.Beginning"/>, when
/// <see cref="MagicAST.AST.References.TimeRelation.Next"/> — the exact GameTime the
/// sibling <see cref="SacrificeAtEndStepTriggeredRule"/> uses.
/// </para>
///
/// <para>
/// "that card" is the just-exiled card from the preceding sentence — NOT free text.
/// It is the linked exiled reference (CR 607.2, the Petravark / Felidar precedent),
/// modelled via <see cref="ObjectFilter.ExiledWith"/> as a
/// <see cref="ObjectReferenceKind.Designated"/> card in the
/// <see cref="Zone.Exile"/> zone exiled with this object
/// (<c>ExiledWith = {Kind: Self}</c>) — a reference, not a threaded runtime binding
/// (ADR 0004 reference-not-resolution). "under its owner's control" rides on
/// <see cref="ReturnToBattlefieldEffect.UnderControl"/> as an
/// <see cref="ObjectReferenceKind.Owner"/> reference (CR 400.6).
/// </para>
///
/// Rule citations: 603.7 (delayed triggers), 513 (End Step), 607.2 (linked exile),
/// 701.13 (exile) / 400.6 (control on return).
/// </summary>
[TriggeredRule]
public sealed class ReturnExiledCardAtNextEndStepTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^return\s+that\s+card\s+to\s+the\s+battlefield\s+under\s+its\s+owner'?s\s+control\s+at\s+the\s+beginning\s+of\s+the\s+next\s+end\s+step$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new MagicAST.AST.Abilities.DelayedTriggeredAbility
      {
        Trigger = new MagicAST.AST.Triggers.TriggerCondition
        {
          Timing = MagicAST.AST.Triggers.TriggerTiming.At,
          Event = new GameTime
          {
            Part = TurnPart.End,
            Edge = TimeBoundary.Beginning,
            When = TimeRelation.Next,
          },
        },
        Effects =
        [
          new ReturnToBattlefieldEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Designated,
              Filter = new ObjectFilter
              {
                Zone = Zone.Exile,
                ExiledWith = new ObjectReference { Kind = ObjectReferenceKind.Self },
              },
            },
            UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
          },
        ],
      },
    };
    return true;
  }
}
