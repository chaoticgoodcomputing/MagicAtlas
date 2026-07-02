namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals N damage to target opponent." — ETB self-ping targeting a single opponent.
/// Covers cards whose triggered ETB ability reads "When this [land|creature|artifact]
/// enters, it deals N damage to target opponent."
///
/// Rule 603: triggered abilities (When/Whenever/At). Rule 120.1-120.2: dealing
/// damage (a source deals damage to a player). Rule 115.1: "target" creates a target.
/// Rule 102.2: an opponent is a player not on the same team as the controller.
///
/// Distinct from <see cref="SelfDealsDamageToAnyTargetTriggeredRule"/> (which targets
/// any legal target — creature, player, or planeswalker — per Rule 115.4) and from
/// <see cref="SelfDealsDamageToYouRule"/> (which targets the controller).
/// </summary>
[TriggeredRule]
public sealed class SelfDealsDamageToOpponentRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^it\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+target\s+opponent\.?$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
    };
    return true;
  }
}
