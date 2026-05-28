namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[CardName] deals N damage to target attacking or blocking creature."
/// — spell analogue of the activated <c>SelfDealsDamageToAttackingOrBlockingCreatureEffectRule</c>.
/// The source is the spell itself (named; encoded as <see cref="ObjectReference.Self()"/>);
/// the target is a creature filtered by Characteristics = ["attacking or blocking"].
/// Covers clean single-line instants/sorceries such as Divine Arrow and Cosmium Blast.
/// </summary>
[SpellRule]
public sealed class SelfDealsDamageToAttackingOrBlockingCreatureRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+target\s+attacking\s+or\s+blocking\s+creature$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    var subject = m.Groups["subject"].Value;
    // Subject must be a capitalised proper-noun (the spell's own name).
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
    {
      return false;
    }

    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = rawAmount switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.Parse(rawAmount),
    };

    effect = new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["attacking or blocking"],
        },
      },
    };
    return true;
  }
}
