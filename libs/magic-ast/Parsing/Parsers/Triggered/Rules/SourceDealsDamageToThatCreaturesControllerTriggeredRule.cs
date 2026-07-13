namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[this source] deals N damage to that creature's controller" — the literal-amount
/// resolution of a "whenever a creature dies" (or other creature-naming) trigger
/// (Dingus Staff: "Whenever a creature dies, this artifact deals 2 damage to that
/// creature's controller."). The source is the permanent itself
/// (<see cref="ObjectReferenceKind.Self"/> — "this artifact"/"this creature"/…); the
/// target is the player who controls the creature named by the trigger condition,
/// encoded as <see cref="ObjectReferenceKind.Controller"/> (the same "that creature's
/// controller" modeling used by <see cref="DealThatMuchDamageToControllerRule"/>).
///
/// <para>
/// The dealt damage is NONCOMBAT damage (CR 120): a triggered ability resolving and
/// dealing damage, not combat damage assigned during the combat damage step (CR 510),
/// so <see cref="DealDamageEffect.IsCombat"/> is left null (≡ noncombat) and omitted
/// from the JSON.
/// </para>
///
/// <para>
/// Distinct from <see cref="DealThatMuchDamageToControllerRule"/> (amount is the
/// "that much" derived quantity of a Repercussion redirection) — here the amount is a
/// literal number stated in the effect text. Distinct from
/// <see cref="ThisCreatureDealsDamageToThatCreatureTriggeredRule"/> (target is the
/// creature itself, <see cref="ObjectReferenceKind.ThatCreature"/>) — here the target
/// is that creature's <em>controller</em>, a player.
/// </para>
///
/// <para>
/// CR 700.4: "The term dies means 'is put into a graveyard from the battlefield.'"
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. … An object that deals damage is the source of that damage." CR 603.2:
/// "Whenever a game event or game state matches a triggered ability's trigger event,
/// that ability automatically triggers." MAST describes; the "that creature's
/// controller" back-reference is a linked reference (ADR 0004), not a threaded binding.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SourceDealsDamageToThatCreaturesControllerTriggeredRule : ITriggeredRule
{
  // Anchored (^…$) and tail-specific to "that creature's controller" so it cannot
  // swallow a more-specific sibling ("… to that player", "… to that creature",
  // or a trailing "and you gain N life"). The source noun phrase generalises over
  // the permanent type words (creature/permanent/artifact/enchantment) and the
  // pronoun "it".
  private static readonly Regex _pattern = new(
    @"^(?:it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+that\s+creature's\s+controller\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.Controller },
    };
    return true;
  }
}
