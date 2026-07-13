namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals N damage divided as you choose among any number of target
/// creatures." — the Fire Covenant divided-damage burn shape. The spell names
/// itself as the source (Rule 113.3a) and deals a single total amount that the
/// controller splits across an unbounded, freely-chosen set of target creatures
/// at announcement, rather than dealing the full amount to each member of the
/// set independently.
///
/// <para>
/// CR 601.2d (verbatim): "If the spell requires the player to divide or
/// distribute an effect (such as damage or counters) among one or more targets,
/// the player announces the division. Each of these targets must receive at
/// least one of whatever is being divided." The division fact rides on
/// <see cref="DealDamageEffect.Divided"/>; the target-set cardinality ("any
/// number of") rides on <see cref="ObjectReference.Quantity"/> as an
/// <see cref="AnyAmountQuantity"/> (CR 107.3 — an upper-unbounded player
/// choice), mirroring <see cref="ReturnCountTargetPermanentsToOwnersHandsSpellRule"/>'s
/// "any number of target [type]" convention.
/// </para>
///
/// <para>
/// The amount is most commonly the additional-cost-defined variable X (CR
/// 601.2b: the controller announces the value of a variable cost as the spell
/// is cast) but a literal/word amount is also recognised for family
/// robustness. Distinct from the sibling <see cref="SelfDealsDamageToTypeDisjunctionRule"/>
/// (which requires "damage to target …" immediately, with no division) —
/// anchored (^…$) on the "damage divided as you choose among any number of
/// target creatures" surface so it cannot collide with that or any other
/// dealDamage sibling.
/// </para>
/// </summary>
[SpellRule]
public sealed class SelfDealsDamageDividedAmongTargetCreaturesRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^(?<subject>\S.*?)\s+deals\s+(?<amount>[XYZ]|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+divided\s+as\s+you\s+choose\s+among\s+any\s+number\s+of\s+target\s+creatures$",
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

    var subject = m.Groups["subject"].Value;
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Amount = ParseAmount(m.Groups["amount"].Value),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
        Quantity = new AnyAmountQuantity(),
      },
      Divided = true,
    };
    return true;
  }

  private static Quantity ParseAmount(string token)
  {
    // Variable placeholder (X/Y/Z) — CR 601.2b: the controller announces the value
    // as the spell is cast, most commonly the value chosen for a linked additional
    // cost's own variable (Fire Covenant's "pay X life").
    if (token.Length == 1 && char.IsLetter(token[0]))
    {
      return new VariableQuantity { Name = token.ToUpperInvariant() };
    }

    if (int.TryParse(token, out var digits))
    {
      return LiteralQuantity.Of(digits);
    }

    var rawAmount = token.ToLowerInvariant();
    int amount = rawAmount switch
    {
      "one" => 1, "two" => 2, "three" => 3, "four" => 4, "five" => 5,
      "six" => 6, "seven" => 7, "eight" => 8, "nine" => 9, "ten" => 10,
      _ => throw new InvalidOperationException("Unreachable: regex only matches recognised amount tokens."),
    };
    return LiteralQuantity.Of(amount);
  }
}
