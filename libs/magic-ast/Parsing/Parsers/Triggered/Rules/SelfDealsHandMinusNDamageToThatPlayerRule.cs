namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this artifact deals X damage to that player, where X is the number of cards
/// in their hand minus N" — the effect clause of an each-opponent's-upkeep
/// trigger whose damage amount is the recipient's hand size reduced by a constant
/// (Iron Maiden: "At the beginning of each opponent's upkeep, this artifact deals
/// X damage to that player, where X is the number of cards in their hand minus
/// 4"). "That player"/"them" and "their hand" both back-reference the opponent
/// whose upkeep fired the trigger (<see cref="ObjectReferenceKind.ThatPlayer"/> /
/// <see cref="ControllerFilter.ThatPlayer"/>), and the source is the card itself
/// (<see cref="ObjectReference.Self"/>) — the same singular back-references the
/// operand-reversed sibling <see cref="SelfDealsThreeMinusHandDamageToThatPlayerRule"/>
/// (Rackling: "X is 3 minus the number of cards in their hand") uses.
///
/// <para>
/// The where-clause resolves X inline to the computed amount. "the number of
/// cards in their hand minus N" is a binary subtraction between two full
/// quantities — a <see cref="CountQuantity"/> over the opponent's hand as the
/// minuend and a <see cref="LiteralQuantity"/> N as the subtrahend — carried by
/// <see cref="CalculatedQuantity"/> with <c>Operation="subtract"</c> and the
/// second operand in <see cref="CalculatedQuantity.OperandQuantity"/>. This
/// mirrors the sibling exactly but with the operands swapped (count minus
/// constant, not constant minus count). The hand-count filter
/// <c>{ card, Owner=ThatPlayer, Zone=Hand }</c> is identical to the sibling's.
/// </para>
///
/// <para>
/// CR 603.2: the event-match (the opponent's upkeep beginning) is the trigger. The
/// damage is dealt by the source to that player (CR 120.1-120.2). Amount is engine
/// territory at resolution (reference-not-resolution, ADR 0004): MAST records the
/// "hand-size minus N" reference, not a pre-resolved number (and it may be zero or
/// less — the engine floors damage at 0 per CR 120.6).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the variable "X damage … where X is the number of cards
/// in … hand minus N" shape is disjoint from the literal-amount
/// <see cref="SelfDealsDamageToThatPlayerRule"/> (which requires a numeric amount
/// immediately before "damage" and ends right after the recipient) and from the
/// operand-reversed <see cref="SelfDealsThreeMinusHandDamageToThatPlayerRule"/>
/// (whose where-clause opens "X is N minus"), so the three never collide.
/// Priority above default so the more-specific variable form is tried first.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class SelfDealsHandMinusNDamageToThatPlayerRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^(?:it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+X\s+damage\s+to\s+"
      + @"(?:them|that\s+player),\s+where\s+X\s+is\s+the\s+number\s+of\s+cards\s+in\s+"
      + @"(?:their|that\s+player's)\s+hand\s+minus\s+(?<n>\d+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var n = int.Parse(m.Groups["n"].Value);

    effect = new DealDamageEffect
    {
      Amount = new CalculatedQuantity
      {
        BaseQuantity = new CountQuantity
        {
          CountOf = new ObjectFilter
          {
            CardTypes = ["card"],
            Owner = ControllerFilter.ThatPlayer,
            Zone = Zone.Hand,
          },
        },
        Operation = "subtract",
        OperandQuantity = LiteralQuantity.Of(n),
      },
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
    };
    return true;
  }
}
