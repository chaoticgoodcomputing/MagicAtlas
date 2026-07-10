namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this enchantment deals damage to that player equal to the number of cards
/// in that player's hand" — the effect clause of an each-opponent's-upkeep
/// trigger whose damage amount is exactly the recipient's hand size, with no
/// further arithmetic (Price of Knowledge). "That player"/"them" and "that
/// player's hand"/"their hand" both back-reference the opponent whose upkeep
/// fired the trigger (<see cref="ObjectReferenceKind.ThatPlayer"/> /
/// <see cref="ControllerFilter.ThatPlayer"/>), and the source is the card
/// itself (<see cref="ObjectReference.Self"/>) — the same singular
/// back-references <see cref="SelfDealsHandMinusNDamageToThatPlayerRule"/> uses.
/// </summary>
/// <remarks>
/// Distinct from <see cref="SelfDealsHandMinusNDamageToThatPlayerRule"/> and
/// <see cref="SelfDealsThreeMinusHandDamageToThatPlayerRule"/>: those cover the
/// "X damage … where X is the number of cards in their hand [minus/plus] N"
/// arithmetic shape (a <see cref="CalculatedQuantity"/>). This rule covers the
/// plain "equal to the number of cards in [their/that player's] hand" shape —
/// a bare <see cref="CountQuantity"/>, no subtraction, no "X … where X is"
/// indirection.
///
/// <para>
/// CR 603.2: the event-match (the opponent's upkeep beginning) is the trigger.
/// The damage is dealt by the source to that player (CR 120.1-120.2). Amount is
/// engine territory at resolution (reference-not-resolution, ADR 0004): MAST
/// records the "equal to hand size" reference, not a pre-resolved number.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the "deals damage to … equal to the number of cards
/// in … hand" shape requires no numeric/variable amount token immediately
/// after "deals", so it is disjoint from the literal-amount
/// <see cref="SelfDealsDamageToThatPlayerRule"/> (which requires a number) and
/// from the "X damage … where X is …" siblings (which require the literal
/// token "X" right after "deals"). The three/four never collide.
/// </para>
/// </remarks>
[TriggeredRule]
public sealed class SelfDealsHandSizeDamageToThatPlayerRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^(?:it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+damage\s+to\s+"
      + @"(?:them|that\s+player)\s+equal\s+to\s+the\s+number\s+of\s+cards\s+in\s+"
      + @"(?:their|that\s+player's)\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Amount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["card"],
          Owner = ControllerFilter.ThatPlayer,
          Zone = Zone.Hand,
        },
      },
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
    };
    return true;
  }
}
