namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[this source] deals N damage to that land's controller" — the literal-amount
/// resolution of a "whenever a land enters" (or other land-naming) trigger
/// (Ankh of Mishra: "Whenever a land enters, this artifact deals 2 damage to that
/// land's controller."). The source is the permanent itself
/// (<see cref="ObjectReferenceKind.Self"/> — "this artifact"/"this creature"/…); the
/// target is the player who controls the land named by the trigger condition,
/// encoded as <see cref="ObjectReferenceKind.Controller"/> (the same "that … 's
/// controller" modeling used by
/// <see cref="SourceDealsDamageToThatCreaturesControllerTriggeredRule"/> and
/// <see cref="DealThatMuchDamageToControllerRule"/>). The possessive noun
/// ("land") merely restates the object the trigger identified; it is inert to the
/// output, which is why this rule emits the identical shape as the creature sibling.
///
/// <para>
/// The dealt damage is NONCOMBAT damage (CR 120): a triggered ability resolving and
/// dealing damage, not combat damage assigned during the combat damage step (CR 510),
/// so <see cref="DealDamageEffect.IsCombat"/> is left null (≡ noncombat) and omitted
/// from the JSON.
/// </para>
///
/// <para>
/// Disjoint from <see cref="SourceDealsDamageToThatCreaturesControllerTriggeredRule"/>
/// (matches "that creature's controller") by the possessive noun, so the two rules
/// never both match the same fragment. Both are anchored (^…$) to the full effect
/// tail so neither can swallow a more-specific sibling ("… to that player",
/// "… to that creature", or a trailing "and you gain N life").
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. … An object that deals damage is the source of that damage." CR 603.2:
/// "Whenever a game event or game state matches a triggered ability's trigger event,
/// that ability automatically triggers." MAST describes; the "that land's controller"
/// back-reference is a linked reference (ADR 0004), not a threaded binding.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SourceDealsDamageToThatLandsControllerTriggeredRule : ITriggeredRule
{
  // Anchored (^…$) and tail-specific to "that land's controller" so it cannot
  // swallow a more-specific sibling. The source noun phrase generalises over the
  // permanent type words (creature/permanent/artifact/enchantment) and the pronoun
  // "it"; the possessive target noun is fixed to "land" (the creature form is the
  // separate SourceDealsDamageToThatCreaturesControllerTriggeredRule).
  private static readonly Regex _pattern = new(
    @"^(?:it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+that\s+land's\s+controller\.?$",
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
