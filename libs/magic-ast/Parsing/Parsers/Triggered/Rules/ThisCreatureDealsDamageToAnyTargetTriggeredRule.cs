namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this creature deals N damage to any target" — self-ping triggered effect
/// that targets any legal target (creature, player, planeswalker, or battle).
///
/// <para>
/// Covers cards like Orcish Bowmasters whose compound-trigger effect reads
/// "this creature deals 1 damage to any target." — where the pronoun is
/// "this creature" rather than "it" (the <see cref="SelfDealsDamageToAnyTargetTriggeredRule"/>
/// pronoun). The source is the creature itself (CR 109); the target is any legal
/// target (CR 115.4: "any target" — player, creature, planeswalker, or battle).
/// </para>
///
/// <para>
/// Rule 120.1–120.2: dealing damage (a source deals damage to a permanent or
/// player). Rule 603.2: triggered abilities (When/Whenever/At).
/// </para>
///
/// <para>
/// Distinct from <see cref="SelfDealsDamageToAnyTargetTriggeredRule"/> (which
/// matches the "it deals" pronoun form); both emit the same <see cref="DealDamageEffect"/>
/// with <see cref="ObjectReference.Self"/> as the source and
/// <see cref="ObjectReferenceKind.AnyTarget"/> as the target.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThisCreatureDealsDamageToAnyTargetTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^this\s+(?:creature|permanent|artifact|enchantment)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+any\s+target\.?$",
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
