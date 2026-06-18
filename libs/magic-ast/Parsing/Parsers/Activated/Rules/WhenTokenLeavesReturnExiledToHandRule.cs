namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When that token leaves the battlefield, put the exiled card into your hand." —
/// creates a <see cref="CreateDelayedTriggerEffect"/> that fires when the token
/// created by the same ability leaves the battlefield, returning the face-down
/// exiled card (exiled "with" this ability's source) to the controller's hand.
///
/// <para>
/// This is the second half of Ugin, the Ineffable's +1 linked-ability pair
/// (CR 406.6: "An object may have one ability that causes one or more cards to be
/// exiled, and another ability that refers either to 'the exiled cards' or to cards
/// 'exiled with [this object]'. These abilities are linked."). The exile is produced
/// by <see cref="ExileTopCardFaceDownAndLookRule"/>; the return is produced here
/// as a delayed trigger keyed on the token's LTB event.
/// </para>
///
/// <para>
/// The trigger filter uses <c>IsToken: true</c> and <c>Controller: You</c> to scope
/// the LTB event to the token just created. The return-to-hand target uses
/// <c>ExiledWith: { Kind: Self }</c> to reference the card exiled by the same source
/// (ADR 0004 reference-not-resolution).
/// </para>
///
/// <para>
/// Fully anchored (^…$). Priority 980 — below the other token-creation and exile
/// rules so those run first; this rule handles the trailing delayed-trigger sentence.
/// </para>
///
/// <para>CR 406.6 (linked abilities); CR 603.7 (delayed triggered abilities).</para>
/// </summary>
[ActivatedEffectRule(Priority = 980)]
public sealed class WhenTokenLeavesReturnExiledToHandRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^\s*When\s+that\s+token\s+leaves\s+the\s+battlefield,\s+put\s+the\s+exiled\s+card\s+into\s+your\s+hand\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText))
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
          Event = TriggerEvent.LeavesTheBattlefield,
          Filter = new ObjectFilter
          {
            IsToken = true,
            Controller = ControllerFilter.You,
          },
        },
        Effects =
        [
          new ReturnToHandEffect
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
          },
        ],
      },
    };
  }
}
