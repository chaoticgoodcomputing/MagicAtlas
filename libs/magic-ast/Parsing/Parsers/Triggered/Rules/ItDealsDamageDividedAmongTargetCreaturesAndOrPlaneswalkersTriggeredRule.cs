namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals N damage divided as you choose among any number of target
/// creatures and/or planeswalkers." — the ETB/death divided-damage sibling of
/// <see cref="Spell.Rules.SelfDealsDamageDividedAmongTargetCreaturesRule"/>
/// (Fire Covenant's spell-level shape), for a triggered ability whose subject
/// is the "it" pronoun rather than the card's own name (Fury: "When this
/// creature enters, it deals 4 damage divided as you choose among any number
/// of target creatures and/or planeswalkers.").
///
/// <para>
/// CR 603.1: triggered abilities have a trigger condition and an effect,
/// written "[When/Whenever/At] [trigger condition], [effect]." CR 601.2d
/// (verbatim): "If the spell requires the player to divide or distribute an
/// effect (such as damage or counters) among one or more targets, the player
/// announces the division. Each of these targets must receive at least one of
/// whatever is being divided." The division fact rides on
/// <see cref="DealDamageEffect.Divided"/>; the target-set cardinality ("any
/// number of") rides on <see cref="ObjectReference.Quantity"/> as an
/// <see cref="AnyAmountQuantity"/> (CR 107.3 — an upper-unbounded player
/// choice). The "creatures and/or planeswalkers" union is a single
/// <see cref="ObjectReferenceKind.Target"/> whose <see cref="ObjectFilter.CardTypes"/>
/// spans both types, mirroring the plain "creature or planeswalker"
/// disjunction convention used by the sibling
/// <see cref="ItDealsDamageToTargetTypeDisjunctionRule"/> (Rule 115.4
/// reasoning generalized to an explicit type union).
/// </para>
///
/// <para>
/// Anchored (^...$) on the full "damage divided as you choose among any
/// number of target creatures and/or planeswalkers" surface so it cannot
/// collide with the plain single-target
/// <see cref="ItDealsDamageToTargetTypeTriggeredRule"/>/<see cref="ItDealsDamageToTargetTypeDisjunctionRule"/>
/// siblings (neither of which mentions "divided").
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ItDealsDamageDividedAmongTargetCreaturesAndOrPlaneswalkersTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^it\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+divided\s+as\s+you\s+choose\s+among\s+any\s+number\s+of\s+target\s+creatures\s+and/or\s+planeswalkers\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = rawAmount switch
    {
      "one" => 1, "two" => 2, "three" => 3, "four" => 4, "five" => 5,
      "six" => 6, "seven" => 7, "eight" => 8, "nine" => 9, "ten" => 10,
      _ => int.Parse(rawAmount),
    };

    effect = new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.It(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature", "planeswalker"] },
        Quantity = new AnyAmountQuantity(),
      },
      Divided = true,
    };
    return true;
  }
}
