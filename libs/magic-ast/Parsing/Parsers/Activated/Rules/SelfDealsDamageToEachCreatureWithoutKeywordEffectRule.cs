namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "This creature deals N damage to each creature without [keyword]." — a one-sided
/// activated-ability sweeper that spares creatures carrying a specific evasion/defensive
/// keyword (Ashen Firebeast: "{1}{R}: This creature deals 1 damage to each creature
/// without flying."). CR 602.1: "Activated abilities have a cost and an effect."; the
/// cost — e.g. "{1}{R}" — is parsed separately by the activated cost rules, this rule
/// recognises only the post-colon effect fragment. CR 120.1: "Objects can deal damage to
/// battles, creatures, planeswalkers, and players. … An object that deals damage is the
/// source of that damage." The source deals non-combat damage simultaneously to every
/// creature (including itself, unless it happens to carry the named keyword) that lacks
/// the named keyword ability.
///
/// <para>
/// The keyword-absence predicate routes to the first-class
/// <see cref="ObjectFilter.LacksKeywords"/> axis for a recognised keyword, per the
/// convention established by <c>CreaturesCantBlockThisTurnRule</c> (Falter) and
/// <c>CreatureWithoutKeywordDiesConditionRule</c> (Luminous Broodmoth family). An
/// unrecognised keyword keeps the honest <see cref="OtherCharacteristic"/> free-text
/// fallback ("withoutX").
/// </para>
///
/// <para>
/// Guard: anchors on "each creature without &lt;keyword&gt;" specifically; does NOT
/// match the unqualified "each creature and each player" shape
/// (<see cref="SelfDealsDamageToEachCreatureAndPlayerEffectRule"/>) or "each opponent"
/// (<see cref="SelfDealsDamageToEachOpponentEffectRule"/>).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class SelfDealsDamageToEachCreatureWithoutKeywordEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^(?<subject>This\s+creature|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+creature\s+without\s+(?<keyword>[A-Za-z]+)$",
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
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.Parse(rawAmount),
    };

    var keyword = m.Groups["keyword"].Value.ToLowerInvariant();
    var filter = Enum.TryParse<KeywordAbility>(keyword, ignoreCase: true, out var kw)
      ? new ObjectFilter { CardTypes = ["creature"], LacksKeywords = [kw] }
      : new ObjectFilter
      {
        CardTypes = ["creature"],
        Characteristics =
        [
          Characteristic.Other($"without{char.ToUpperInvariant(keyword[0])}{keyword[1..]}"),
        ],
      };

    return new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = filter,
      },
    };
  }
}
