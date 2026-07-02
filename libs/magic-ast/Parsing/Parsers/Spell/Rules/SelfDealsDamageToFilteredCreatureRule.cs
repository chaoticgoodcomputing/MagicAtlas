namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals N damage to target/each [type] with [characteristic]." — Take Down's
/// modal options. Distinct from the bare type-disjunction shape: this requires a
/// trailing "with [characteristic]" qualifier. (Source-order pair noted at
/// SpellAbilityParser.cs:592 — both rules currently disjoint via the "with" anchor.)
/// </summary>
[SpellRule]
public sealed class SelfDealsDamageToFilteredCreatureRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+(?<det>target|each)\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)\s+(?<chars>with\s+\S.*?)$",
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

    var kind = m.Groups["det"].Value.Equals("each", System.StringComparison.OrdinalIgnoreCase)
      ? ObjectReferenceKind.Each
      : ObjectReferenceKind.Target;

    effect = new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = kind,
        Filter = new ObjectFilter
        {
          CardTypes = [m.Groups["type"].Value.ToLowerInvariant()],
          Characteristics = [Characteristic.FromLabel(m.Groups["chars"].Value.Trim())],
        },
      },
    };
    return true;
  }
}
