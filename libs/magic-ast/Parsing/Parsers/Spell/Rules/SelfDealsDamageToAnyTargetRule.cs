namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals N damage to any target." — bare self-by-name dealDamage with
/// AnyTarget. Covers clean burn spells like Open Fire, Shock, Lightning Strike
/// where the spell names itself as the source and targets "any target" (player,
/// creature, or planeswalker) without any type filter. Distinct from
/// <see cref="SelfDealsDamageToTypeDisjunctionRule"/> (which requires a specific
/// type or type disjunction) and <see cref="SelfDealsDamageToFilteredCreatureRule"/>
/// (which requires a trailing "with [characteristic]" qualifier).
/// </summary>
[SpellRule]
public sealed class SelfDealsDamageToAnyTargetRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+any\s+target$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    var subject = m.Groups["subject"].Value;
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
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
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
    return true;
  }
}
