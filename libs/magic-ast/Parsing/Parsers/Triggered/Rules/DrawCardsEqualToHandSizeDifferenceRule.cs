namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "draw cards equal to the difference" — the effect half of The Ten Rings'
/// end-step top-up ability: "At the beginning of your end step, if you have
/// fewer than ten cards in hand, draw cards equal to the difference." "The
/// difference" is the gap between the intervening-if's hand-size threshold
/// (ten) and the player's current hand size — CR 402.2 (maximum hand size).
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TriggeredAbilityParser"/> pipeline extracts the intervening-if
/// ("if you have fewer than ten cards in hand") into a separate
/// <see cref="MagicAST.AST.Abilities.Condition"/> BEFORE dispatching the
/// remaining effect fragment ("draw cards equal to the difference") to the
/// registry of <see cref="ITriggeredRule"/>s — this rule never sees the "ten"
/// literal directly. It reads the threshold off
/// <see cref="MagicAST.Parsing.ConditionParser.PendingHandSizeUpperBound"/>,
/// which <see cref="MagicAST.Parsing.ConditionParser.Parse"/> populates moments
/// earlier while building that same intervening-if (same synchronous ability
/// parse; the pending value is cleared immediately on read). Without a
/// preceding hand-size intervening-if, there is no threshold to subtract from,
/// so the rule declines (returns <see langword="false"/>) rather than guess.
/// </para>
///
/// <para>
/// The quantity is fully structured — <see cref="CalculatedQuantity"/> with the
/// literal threshold as <see cref="CalculatedQuantity.BaseQuantity"/>,
/// <c>Operation="subtract"</c>, and a <see cref="DerivedQuantity"/> keyed on
/// <see cref="DerivedKind.CardsInHand"/> as
/// <see cref="CalculatedQuantity.OperandQuantity"/> — mirroring the "N minus the
/// number of cards in … hand" shape <c>SelfDealsThreeMinusHandDamageToThatPlayerRule</c>
/// and Iron Maiden's where-clause use for the identical binary-subtraction idiom.
/// Reference-not-resolution (ADR 0004): the engine reads the current hand size at
/// resolution, MAST does not pre-evaluate it.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the fixed phrase "draw cards equal to the difference"
/// does not collide with any numbered-count draw rule (which all require a
/// digit/count-word immediately after "draw").
/// </para>
/// </remarks>
[TriggeredRule(Priority = 955)]
public sealed class DrawCardsEqualToHandSizeDifferenceRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^draw\s+cards\s+equal\s+to\s+the\s+difference\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    var threshold = MagicAST.Parsing.ConditionParser.PendingHandSizeUpperBound;
    MagicAST.Parsing.ConditionParser.PendingHandSizeUpperBound = null;
    if (threshold is not int n)
    {
      return false;
    }

    effect = new DrawCardsEffect
    {
      Count = new CalculatedQuantity
      {
        BaseQuantity = LiteralQuantity.Of(n),
        Operation = "subtract",
        OperandQuantity = new DerivedQuantity { DerivedFrom = DerivedKind.CardsInHand },
      },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
