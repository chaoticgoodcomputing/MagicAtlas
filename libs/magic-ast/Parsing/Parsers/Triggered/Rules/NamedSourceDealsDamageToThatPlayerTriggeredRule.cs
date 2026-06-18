namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[CardName] deals N damage to that player." — named-source self-ping effect
/// directed at the trigger's named player. Covers triggered abilities whose effect
/// reads "[CardName] deals N damage to that player." where the card refers to
/// itself by its own printed name (CR 201.4 — a card's name in its own text refers
/// to itself). MAST resolves the self-reference to
/// <see cref="ObjectReferenceKind.Self"/>. "That player" back-references whichever
/// player caused the trigger to fire (CR 603.2: the event-match player).
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players." CR 603.1: "Triggered abilities have a trigger condition and an
/// effect."
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent substring matches. Priority 62 — just above
/// <see cref="NamedSourceDealsDamageToAnyTargetTriggeredRule"/> (Priority 60) so
/// the more-specific "that player" target is claimed before the "any target" form.
/// Distinct from <see cref="SelfDealsDamageToThatPlayerRule"/>, which handles the
/// pronoun form "this creature/it deals N damage to that player/them".
/// </para>
/// </summary>
[TriggeredRule(Priority = 62)]
public sealed class NamedSourceDealsDamageToThatPlayerTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^(?<subject>[A-Z]\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+that\s+player\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    // Subject must begin with a capital letter — card names are capitalised (CR 201.4).
    var subject = m.Groups["subject"].Value;
    if (!char.IsUpper(subject[0]))
    {
      return false;
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

    effect = new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
    };
    return true;
  }
}
