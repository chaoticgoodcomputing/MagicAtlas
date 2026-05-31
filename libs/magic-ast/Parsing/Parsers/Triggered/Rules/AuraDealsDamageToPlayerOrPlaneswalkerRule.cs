namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this Aura deals N damage to that player or a planeswalker that player
/// controls." — the upkeep-damage trigger of a player-enchanting Curse (Curse of
/// the Pierced Heart). The "or" is a single chooser-selected reference, so the
/// damage Target is a <see cref="ObjectReferenceKind.Choice"/> over two options:
/// the enchanted player ("that player", on a player Aura = the enchanted player)
/// and a planeswalker that player controls.
///
/// <para>
/// Rule 120 (dealing damage); Rule 702.5 (Enchant player — the enchanted player is
/// the Aura's enchanted object). The chooser picks exactly one option at
/// resolution, so a single Choice reference is correct rather than two targets.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class AuraDealsDamageToPlayerOrPlaneswalkerRule : ITriggeredRule
{
  // "[subject] deals N damage to that player or a planeswalker that player controls"
  private static readonly Regex Pattern = new(
    @"^.+?\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+that\s+player\s+or\s+a\s+planeswalker\s+that\s+player\s+controls$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
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
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Choice,
        Options =
        [
          new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["planeswalker"],
              Controller = ControllerFilter.EnchantedPlayer,
            },
          },
        ],
      },
    };
    return true;
  }
}
