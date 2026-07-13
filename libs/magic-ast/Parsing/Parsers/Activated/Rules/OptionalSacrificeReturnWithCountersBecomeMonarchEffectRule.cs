namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Heart-Shaped Herb's activated payoff: "You may sacrifice a creature. If you do,
/// return that card to the battlefield under its owner's control with three +1/+1
/// counters on it and you become the monarch."
///
/// CR 118.12 (the "you may … if you do" idiom): "[A player] may [do something]. If
/// [that player] [does, doesn't, or can't], [effect]." — modelled as an
/// <see cref="OptionalEffect"/> whose <c>Inner</c> is the optional sacrifice and
/// whose <c>IfYouDo</c> is the gated payoff.
///
/// CR 725 (the monarch): "The monarch is a desig­nation a player can have… only one
/// player is the monarch at any given time." — the "you become the monarch" clause
/// is a <see cref="BecomeMonarchEffect"/> naming the ability's controller (You).
///
/// Decomposition:
/// <list type="bullet">
///   <item><c>OptionalEffect.Inner</c> = "sacrifice a creature": a
///   <see cref="SacrificeEffect"/> whose <c>Target</c> is the sacrificing player
///   (You) and whose <c>Filter</c> is the sacrificed object class (creature) —
///   mirroring Diabolic Edict's "[player] sacrifices a creature" shape.</item>
///   <item><c>OptionalEffect.IfYouDo</c> = a <see cref="CompositeEffect"/> of the two
///   gated instructions:
///     <list type="bullet">
///       <item>"return that card to the battlefield under its owner's control with
///       three +1/+1 counters on it": a <see cref="ReturnToBattlefieldEffect"/> whose
///       <c>Target</c> is the sacrificed card (It), <c>UnderControl</c> is its Owner,
///       and <c>WithCounters</c> places three +1/+1 counters (mirroring Persist's
///       "return it … under its owner's control with a -1/-1 counter on it").</item>
///       <item>"and you become the monarch": a <see cref="BecomeMonarchEffect"/> for
///       You.</item>
///     </list>
///   </item>
/// </list>
///
/// ANCHORED (^...$): the full effect sentence is matched in its entirety, so this
/// rule cannot fire as a substring of a broader clause.
/// </summary>
[ActivatedEffectRule(Priority = 995)]
public sealed class OptionalSacrificeReturnWithCountersBecomeMonarchEffectRule
  : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+sacrifice\s+a\s+creature\.\s*If\s+you\s+do,\s*return\s+that\s+card\s+to\s+the\s+battlefield\s+under\s+its\s+owner's\s+control\s+with\s+three\s+\+1/\+1\s+counters\s+on\s+it\s+and\s+you\s+become\s+the\s+monarch\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText))
    {
      return null;
    }

    return new OptionalEffect
    {
      Inner = new SacrificeEffect
      {
        Target = ObjectReference.You(),
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      IfYouDo = new CompositeEffect
      {
        Effects =
        [
          new ReturnToBattlefieldEffect
          {
            Target = ObjectReference.It(),
            Tapped = false,
            UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
            WithCounters = new CounterPlacement
            {
              CounterType = "+1/+1",
              Count = LiteralQuantity.Of(3),
            },
          },
          new BecomeMonarchEffect { Player = ObjectReference.You() },
        ],
      },
    };
  }
}
