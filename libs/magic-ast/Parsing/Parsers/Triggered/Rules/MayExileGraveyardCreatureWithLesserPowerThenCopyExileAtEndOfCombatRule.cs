namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "you may exile target creature card with lesser power from your graveyard. If
/// you do, create a token that's a copy of that card and that's tapped and
/// attacking. Exile the token at end of combat." — Gyrus, Waker of Corpses's
/// attack-trigger resolution body (CR 508.1k covers a token entering as an
/// attacking creature; CR 702.134 is the "lesser power" comparison family shared
/// with Mentor).
///
/// <para>
/// One <see cref="OptionalEffect"/> (ADR 0005 "you may"): <see cref="OptionalEffect.Inner"/>
/// is the graveyard exile (<see cref="ExileEffect"/>, <c>Target</c> filtered to
/// <c>Zone.Graveyard</c>, <c>Controller.You</c>, and a <see cref="MagicAST.AST.References.Comparison"/>
/// on <c>PowerComparison</c> — <c>LessThan</c> relative to <see cref="ObjectReferenceKind.Self"/>'s
/// power, mirroring Mentor/Hammer Dropper's "with lesser power" shape).
/// <see cref="OptionalEffect.IfYouDo"/> is a <see cref="CompositeEffect"/> of the two
/// consequences that only make sense once a card was actually exiled:
/// <list type="number">
///   <item>"create a token that's a copy of that card" — a <see cref="CopyEffect"/>
///   whose <c>Target</c> is <see cref="ObjectReferenceKind.It"/>, the anaphoric
///   back-reference to the just-exiled card (mirrors
///   <see cref="Activated.Rules.CopyExiledCardAndCastWithoutPayingEffectRule"/>'s
///   "the copy" → <c>It</c> convention). "and that's tapped and attacking" is the
///   contextually-implicit qualifier already established as NOT a separate
///   <see cref="CopyEffect"/> field by <see cref="CreateTappedAttackingCopyOfNontokenCreatureRule"/>
///   (Satya, Aetherflux Genius's gold fixture).</item>
///   <item>"Exile the token at end of combat." — a
///   <see cref="CreateDelayedTriggerEffect"/> (CR 603.7) whose clock point is
///   <c>GameTime{Part:Combat, Edge:End}</c> ("at end of combat" — the same
///   <c>GameTime</c> shape <see cref="RemoveCountersTriggeredRule"/> uses for the
///   identical clock phrase), exiling <see cref="ObjectReferenceKind.It"/> — the
///   anaphoric back-reference to the token just created by the sibling effect
///   above (mirrors <see cref="SacrificeAtEndStepTriggeredRule"/>'s "Sacrifice it
///   at the beginning of the next end step" pattern for a token created earlier
///   in the same resolution).</item>
/// </list>
/// The delayed exile is nested inside <c>IfYouDo</c> (not a third top-level
/// sibling effect) because it is only meaningful when the token from step 1
/// exists — both consequences flow from the same "if you do" branch.
/// </para>
///
/// <para>
/// Implemented as a single <see cref="ITriggeredRule"/> matching the WHOLE
/// three-sentence effect body (periods included, final period stripped by the
/// dispatcher) because the "If you do" consequence must stay paired with its
/// governing "you may" — the dispatcher's generic sentence-bundle splitter
/// (<see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/>) tries each
/// "<c>. </c>"-delimited sentence independently and would otherwise strand the
/// "If you do, ..." clause from its antecedent "you may exile ..." clause.
/// </para>
///
/// <para>
/// ANCHORED (^…$) to the exact card-specific surface, so it cannot collide with
/// any other "you may exile ... from your graveyard" sibling (none of which pair
/// with this "create a copy token, then delayed-exile it at end of combat"
/// follow-up). CR 701.13 (exile); CR 111.1 (token creation); CR 707.2 (copy
/// semantics); CR 603.7 (delayed triggered abilities); CR 117.7/CR 118.12 ("you
/// may").
/// </para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class MayExileGraveyardCreatureWithLesserPowerThenCopyExileAtEndOfCombatRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+exile\s+target\s+creature\s+card\s+with\s+lesser\s+power\s+from\s+your\s+graveyard\.\s*"
      + @"If\s+you\s+do,\s*create\s+a\s+token\s+that'?s\s+a\s+copy\s+of\s+that\s+card\s+and\s+that'?s\s+tapped\s+and\s+attacking\.\s*"
      + @"Exile\s+the\s+token\s+at\s+end\s+of\s+combat\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Zone = Zone.Graveyard,
            Controller = ControllerFilter.You,
            PowerComparison = new Comparison
            {
              Operator = ComparisonOperator.LessThan,
              RelativeTo = ObjectReference.Self(),
              RelativeCharacteristic = RelativeCharacteristic.Power,
            },
          },
        },
      },
      IfYouDo = new CompositeEffect
      {
        Effects =
        [
          new CopyEffect { Target = ObjectReference.It() },
          new CreateDelayedTriggerEffect
          {
            DelayedTrigger = new DelayedTriggeredAbility
            {
              Trigger = new TriggerCondition
              {
                Timing = TriggerTiming.At,
                Event = new GameTime { Part = TurnPart.Combat, Edge = TimeBoundary.End },
              },
              Effects = [new ExileEffect { Target = ObjectReference.It() }],
            },
          },
        ],
      },
    };
    return true;
  }
}
