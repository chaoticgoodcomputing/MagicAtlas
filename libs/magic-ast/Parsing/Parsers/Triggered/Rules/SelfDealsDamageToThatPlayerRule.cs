namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this creature deals N damage to them" — the effect clause of an upkeep
/// trigger whose recipient is the player named by the trigger condition
/// (Prickle Faeries: "At the beginning of each opponent's upkeep, …, this
/// creature deals 2 damage to them"). "Them" is the singular pronoun for that
/// opponent — the player whose upkeep fired the trigger — and maps to
/// <see cref="ObjectReferenceKind.ThatPlayer"/> (the same back-reference
/// <see cref="ThatPlayerLosesLifeRule"/> uses). The source is the card itself,
/// <see cref="ObjectReference.Self"/>.
///
/// <para>
/// CR 603.2: the event-match (the opponent's upkeep beginning) is the trigger.
/// The damage is dealt by the source to that player (CR 120.1-120.2). Distinct
/// from <see cref="SelfDealsDamageToYouRule"/> (recipient = the controller) and
/// <see cref="SelfDealsDamageToOpponentRule"/> ("it deals … to target opponent",
/// a fresh targeted opponent rather than the trigger's named player).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SelfDealsDamageToThatPlayerRule : ITriggeredRule
{
  // "this creature deals 2 damage to them" — the recipient pronoun is "them"
  // (or "that player"), back-referencing the trigger's named player.
  private static readonly Regex _pattern = new(
    @"^(?:it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+(?:them|that\s+player)$",
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

    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
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
      _ => int.Parse(raw),
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
