namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this creature deals X damage to that player, where X is N minus the number
/// of cards in their hand" — the effect clause of an each-opponent's-upkeep
/// trigger whose damage amount is a constant reduced by the recipient's hand
/// size (Rackling: "At the beginning of each opponent's upkeep, this creature
/// deals X damage to that player, where X is 3 minus the number of cards in
/// their hand"). "That player"/"them" and "their hand" both back-reference the
/// opponent whose upkeep fired the trigger (<see cref="ObjectReferenceKind.ThatPlayer"/> /
/// <see cref="ControllerFilter.ThatPlayer"/>) — the same singular back-reference the
/// literal-amount sibling <see cref="SelfDealsDamageToThatPlayerRule"/> uses. The
/// source is the card itself (<see cref="ObjectReference.Self"/>).
///
/// <para>
/// The where-clause resolves X inline to the computed amount (the same "X is …"
/// inlining <see cref="ModifyPTByDieResultTriggeredRule"/> performs). "N minus the
/// number of cards in their hand" is a binary subtraction between two full quantities —
/// a <see cref="LiteralQuantity"/> N as the minuend and a <see cref="CountQuantity"/>
/// over the opponent's hand as the subtrahend — carried by
/// <see cref="CalculatedQuantity"/> with <c>Operation="subtract"</c> and the second
/// operand in <see cref="CalculatedQuantity.OperandQuantity"/> (the scalar
/// <see cref="CalculatedQuantity.Operand"/> cannot hold a game-state count). The
/// hand-count filter <c>{ card, Owner=ThatPlayer, Zone=Hand }</c> mirrors the
/// intervening-if filter Prickle Faeries uses for the identical "cards in their
/// hand" concept.
/// </para>
///
/// <para>
/// CR 603.2: the event-match (the opponent's upkeep beginning) is the trigger. The
/// damage is dealt by the source to that player (CR 120.1–120.2). Amount is engine
/// territory at resolution (reference-not-resolution, ADR 0004): MAST records the
/// "N minus hand-size" reference, not a pre-resolved number (and it may be zero or
/// less — the engine floors damage at 0 per CR 120.6).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the variable "X damage … where X is N minus the number of
/// cards in … hand" shape is disjoint from the literal-amount
/// <see cref="SelfDealsDamageToThatPlayerRule"/> (which requires a numeric amount
/// immediately before "damage" and ends right after the recipient), so the two never
/// collide. Priority above default so the more-specific variable form is tried first.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class SelfDealsThreeMinusHandDamageToThatPlayerRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^(?:it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+X\s+damage\s+to\s+"
      + @"(?:them|that\s+player),\s+where\s+X\s+is\s+(?<n>\d+)\s+minus\s+the\s+number\s+of\s+cards\s+in\s+"
      + @"(?:their|that\s+player's)\s+hand$",
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
        BaseQuantity = LiteralQuantity.Of(n),
        Operation = "subtract",
        OperandQuantity = new CountQuantity
        {
          CountOf = new ObjectFilter
          {
            CardTypes = ["card"],
            Owner = ControllerFilter.ThatPlayer,
            Zone = Zone.Hand,
          },
        },
      },
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
    };
    return true;
  }
}
