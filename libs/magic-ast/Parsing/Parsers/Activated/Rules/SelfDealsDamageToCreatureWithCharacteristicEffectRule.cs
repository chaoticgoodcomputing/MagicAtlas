namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[This creature|CardName] deals N damage to target/each creature with [characteristic]."
/// — the flying-hoser archer pattern (Centaur Archer: "{T}: This creature deals 1
/// damage to target creature with flying."). Sibling of
/// <see cref="SelfDealsDamageToAttackingOrBlockingCreatureEffectRule"/> and the
/// spell-side <see cref="Spell.Rules.SelfDealsDamageToFilteredCreatureRule"/>: both
/// map to self-as-source dealDamage, differing only in the qualifier used to filter
/// the creature target. Anchored on a trailing "with [characteristic]" clause so it
/// cannot claim the bare "target creature" shape (that's
/// <see cref="SelfDealsDamageToAnyTargetEffectRule"/>'s TargetTypePattern, which is
/// itself anchored to end right after the type word).
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class SelfDealsDamageToCreatureWithCharacteristicEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^(?<subject>This\s+creature|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+(?<det>target|each)\s+creature\s+(?<chars>with\s+\S.*?)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Pattern.Match(trimmed);
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

    var kind = m.Groups["det"].Value.Equals("each", StringComparison.OrdinalIgnoreCase)
      ? ObjectReferenceKind.Each
      : ObjectReferenceKind.Target;

    return new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = kind,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.FromLabel(m.Groups["chars"].Value.Trim())],
        },
      },
    };
  }
}
