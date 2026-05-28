namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[This creature|CardName] deals N damage to target attacking or blocking creature."
/// — archer / ballista tap-to-deal-damage pattern. Both subject forms encode the
/// source as <see cref="ObjectReference.Self()"/>; the target is a creature with
/// Characteristics = ["attacking or blocking"].
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class SelfDealsDamageToAttackingOrBlockingCreatureEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Regex.Match(
      trimmed,
      @"^(?<subject>This\s+creature|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+target\s+attacking\s+or\s+blocking\s+creature$",
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
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["attacking or blocking"],
        },
      },
    };
  }
}
