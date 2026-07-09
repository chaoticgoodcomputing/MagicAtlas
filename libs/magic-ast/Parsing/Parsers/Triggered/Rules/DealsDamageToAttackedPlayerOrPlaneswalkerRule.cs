namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[this enchantment|this creature|Name] deals N damage to the player or planeswalker
/// that creature is attacking." — the attack-trigger burn family (Cavalcade of Calamity:
/// "Whenever a creature you control with power 1 or less attacks, this enchantment deals 1
/// damage to the player or planeswalker that creature is attacking.").
///
/// <para>Emits a <see cref="DealDamageEffect"/> (Rule 120 — non-combat damage dealt by a
/// resolving triggered ability) whose <see cref="DealDamageEffect.Target"/> is a
/// <see cref="ObjectReferenceKind.AttackedPlayerOrPlaneswalker"/> reference: the single
/// defending object (a player OR the attacked planeswalker) that the trigger's attacking
/// creature is attacking (CR 508.1b). The "or" is the two possible kinds of that one
/// determined object, not a chooser's alternatives, so it is a single determined reference
/// rather than a <see cref="ObjectReferenceKind.Choice"/>. The damage <see cref="DealDamageEffect.Source"/>
/// is <see cref="ObjectReferenceKind.Self"/> — "this enchantment"/"this creature" is the
/// ability's own source object (CR 109).</para>
///
/// <para>The tail "the player or planeswalker that creature is attacking" is anchored on the
/// right (^…$) so this rule cannot claim a differently-shaped damage effect; the subject is
/// captured but always resolves to the source object (Self), because a resolving ability's
/// damage is dealt by the permanent that has the ability.</para>
/// </summary>
[TriggeredRule]
public sealed class DealsDamageToAttackedPlayerOrPlaneswalkerRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^.+?\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+the\s+player\s+or\s+planeswalker\s+that\s+creature\s+is\s+attacking$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.AttackedPlayerOrPlaneswalker },
      Source = ObjectReference.Self(),
    };
    return true;
  }
}
