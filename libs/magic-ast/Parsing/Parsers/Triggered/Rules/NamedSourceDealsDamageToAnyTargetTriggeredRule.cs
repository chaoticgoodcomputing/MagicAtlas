namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[CardName] deals N damage to any target." — named-source self-ping triggered
/// effect. Covers cards whose triggered ability effect reads "[CardName] deals N
/// damage to any target." where the card refers to itself by its own printed name
/// (Rule 201.4 — a card's name refers to itself when used in its own text).
/// MAST resolves the self-reference to <see cref="ObjectReferenceKind.Self"/>.
///
/// Rule 120.1-120.2: dealing damage (a source deals damage to a permanent/player).
/// Rule 115.4: "any target" — a target may be any player, creature, planeswalker,
/// or battle unless otherwise restricted.
///
/// Distinct from <see cref="SelfDealsDamageToAnyTargetTriggeredRule"/> (which
/// handles the pronoun "it deals N damage" shape — same event, different oracle
/// phrasing). Both map to Source=Self; the difference is naming convention only.
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class NamedSourceDealsDamageToAnyTargetTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^(?<subject>[A-Z]\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+any\s+target\.?$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    // Subject must start with a capital letter — card names are capitalised.
    var subject = m.Groups["subject"].Value;
    if (!char.IsUpper(subject[0]))
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
