namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[This creature|CardName] deals N damage to target player or planeswalker."
/// — the Heartwood Giant / firebreathing-adjacent ping pattern. Both subject
/// forms encode the source as <see cref="ObjectReference.Self()"/>; the target
/// is a single <see cref="ObjectReference"/> whose <see cref="ObjectFilter"/>
/// carries a two-entry <c>CardTypes</c> union — the same "X or Y" disjunction
/// shape used by the triggered-ability sibling
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.ItDealsDamageToTargetTypeDisjunctionRule"/>
/// (CR 115.4 reasoning generalized to an explicit two-type union; "player" and
/// "planeswalker" are both legal recipients of damage per CR 120.1).
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class SelfDealsDamageToPlayerOrPlaneswalkerEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Regex.Match(
      trimmed,
      @"^(?<subject>This\s+creature|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+target\s+player\s+or\s+planeswalker$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    // Accept "This creature" (pronoun self-ref) or a capitalised proper-noun subject.
    var subject = m.Groups["subject"].Value;
    var isThisCreature = subject.Equals("This creature", StringComparison.OrdinalIgnoreCase);
    var isNamedSelf = subject.Length > 0 && char.IsUpper(subject[0]) && !isThisCreature;
    if (!isThisCreature && !isNamedSelf)
    {
      return null;
    }

    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = rawAmount switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(rawAmount),
    };

    return new MagicAST.AST.Effects.Damage.DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player", "planeswalker"] },
      },
    };
  }
}
