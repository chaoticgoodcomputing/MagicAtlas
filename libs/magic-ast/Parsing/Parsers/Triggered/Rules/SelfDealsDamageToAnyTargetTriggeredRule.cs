namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals N damage to any target." — ETB self-ping targeting any legal target
/// (creature, player, or planeswalker). Covers cards whose triggered ETB ability
/// reads "When this [creature|artifact] enters, it deals N damage to any target."
///
/// Rule 603: triggered abilities (When/Whenever/At). Rule 120.1-120.2: dealing
/// damage (a source deals damage to a permanent/player). Rule 115.4: "any target"
/// — a target may be any player, creature, planeswalker, or battle unless
/// otherwise restricted.
///
/// Distinct from <see cref="SelfDealsDamageToYouRule"/> (targets the controller only)
/// and the spell-side <see cref="Spell.Rules.SelfDealsDamageToAnyTargetRule"/>
/// (subject is the spell itself, not a triggered pronoun).
/// </summary>
[TriggeredRule]
public sealed class SelfDealsDamageToAnyTargetTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^it\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+any\s+target\.?$",
      RegexOptions.IgnoreCase
    );
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
      Source = ObjectReference.It(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
    return true;
  }
}
