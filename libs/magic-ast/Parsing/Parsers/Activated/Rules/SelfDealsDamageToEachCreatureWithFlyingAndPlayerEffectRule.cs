namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "This creature deals N damage to each creature with [keyword] and each player." — the
/// filtered sibling of <see cref="SelfDealsDamageToEachCreatureAndPlayerEffectRule"/> where the
/// creature half of the symmetric ping is restricted to creatures that <em>have</em> a keyword
/// ability (Rockcaster Platoon: "{4}{G}: This creature deals 2 damage to each creature with flying
/// and each player."). CR 602.1: "Activated abilities have a cost and an effect."; the cost — e.g.
/// "{4}{G}" — is parsed separately by the activated cost rules, this rule recognises only the
/// post-colon effect fragment. CR 120.1: "Objects can deal damage to battles, creatures,
/// planeswalkers, and players. … An object that deals damage is the source of that damage." The
/// source deals non-combat damage (CR 120.1) simultaneously to every creature carrying the named
/// keyword and to every player, the latter causing life loss (CR 119.2).
///
/// <para>
/// Emits a <see cref="CompositeEffect"/> of two <see cref="DealDamageEffect"/> nodes — one to the
/// filtered creatures, one to <see cref="ObjectReferenceKind.EachPlayer"/> — mirroring the shape of
/// <see cref="SelfDealsDamageToEachCreatureAndPlayerEffectRule"/>. The "with [keyword]" predicate is
/// structured through <see cref="Characteristic.FromLabel"/>, which yields a first-class
/// <see cref="KeywordCharacteristic"/> for keyword abilities this AST models (flying, reach, …) and
/// falls back to the typed <see cref="OtherCharacteristic"/> residual otherwise.
/// </para>
///
/// <para>
/// Guard: ANCHORED (<c>^…$</c>) and REQUIRES "each creature with &lt;keyword&gt; and each player".
/// It cannot claim the unqualified "each creature and each player" shape
/// (<see cref="SelfDealsDamageToEachCreatureAndPlayerEffectRule"/> — no "with" clause), the
/// "each creature without &lt;keyword&gt;" sweeper
/// (<see cref="SelfDealsDamageToEachCreatureWithoutKeywordEffectRule"/>), or the "each opponent"
/// form (<see cref="SelfDealsDamageToEachOpponentEffectRule"/>). Conversely those anchored siblings
/// never see this sentence because their patterns lack the "with &lt;keyword&gt; and each player"
/// tail.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 991)]
public sealed class SelfDealsDamageToEachCreatureWithFlyingAndPlayerEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^(?<subject>This\s+creature|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+creature\s+with\s+(?<keyword>[A-Za-z]+)\s+and\s+each\s+player$",
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

    var keyword = m.Groups["keyword"].Value;

    return new CompositeEffect
    {
      Effects =
      [
        new DealDamageEffect
        {
          Amount = LiteralQuantity.Of(amount),
          Source = ObjectReference.Self(),
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Characteristics = [Characteristic.FromLabel(keyword)],
            },
          },
        },
        new DealDamageEffect
        {
          Amount = LiteralQuantity.Of(amount),
          Source = ObjectReference.Self(),
          Target = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
        },
      ],
    };
  }
}
