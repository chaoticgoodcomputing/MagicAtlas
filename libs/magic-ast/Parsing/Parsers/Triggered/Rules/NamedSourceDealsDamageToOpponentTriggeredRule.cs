namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[CardName] deals N damage to target opponent." — named-source triggered
/// effect directed at a single opponent target. Covers triggered abilities
/// whose effect reads "[CardName] deals N damage to target opponent." where
/// the card refers to itself by its own printed name (CR 201.5 — a card's name
/// in its own text refers to itself). MAST resolves the self-reference to
/// <see cref="ObjectReferenceKind.Self"/>. Gev, Scaled Scorch: "Whenever you
/// cast a Lizard spell, Gev deals 1 damage to target opponent."
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players." CR 603.1: "Triggered abilities have a trigger condition and an
/// effect." CR 115.1: "target" creates a target; the affected player is
/// <see cref="ObjectReferenceKind.Opponent"/> ("an opponent", "target opponent").
/// </para>
///
/// <para>
/// Sibling to <see cref="NamedSourceDealsDamageToAnyTargetTriggeredRule"/>
/// (Priority 60, "any target") and
/// <see cref="NamedSourceDealsDamageToThatPlayerTriggeredRule"/> (Priority 62,
/// "that player") — same named-source-self-reference convention, different
/// target shape ("target opponent" specifically, distinct from the unrestricted
/// "any target" and the back-referenced "that player"). Anchored (^…$) so it
/// cannot steal either sibling's surface. Distinct from
/// <see cref="SelfDealsDamageToOpponentRule"/>, which handles the pronoun form
/// "it deals N damage to target opponent" rather than the literal card name.
/// </para>
/// </summary>
[TriggeredRule(Priority = 62)]
public sealed class NamedSourceDealsDamageToOpponentTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^(?<subject>[A-Z]\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+target\s+opponent\.?$",
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

    // Subject must begin with a capital letter — card names are capitalised (CR 201.5).
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
    };
    return true;
  }
}
