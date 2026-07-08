namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals N damage to target [type]." — the single-type sibling of the ETB/death
/// damage family. Covers triggered abilities such as "When this creature dies, it
/// deals 2 damage to target creature." (Bogardan Firefiend) and "When this creature
/// enters, it deals 2 damage to target creature." (Fire Imp, Corrupt Eunuchs,
/// Goblin Commando).
///
/// Rule 603.1: triggered abilities have a trigger condition and an effect, written
/// "[When/Whenever/At] [trigger condition], [effect]." Rule 120.1-120.2: any object
/// can deal damage to a battle, creature, planeswalker, or player; the object that
/// deals damage is the source of that damage. Rule 115.1: "target" creates a target
/// that must be declared as the ability is put on the stack.
///
/// The subject is the triggered pronoun "it" (referring back to the trigger's
/// source), modelled as <see cref="ObjectReferenceKind.It"/> — the same convention
/// used by the sibling rules below.
///
/// REQUIRES a single bare type with no trailing qualifier ("or …", "with …", "an
/// opponent controls", "it's blocking") and no leading conditional clause — those
/// shapes are handled elsewhere or remain unstructured:
/// <list type="bullet">
///   <item><see cref="ItDealsDamageToTargetTypeDisjunctionRule"/> — requires "or"
///   joining two types ("target creature or planeswalker").</item>
///   <item><see cref="SelfDealsDamageToOpponentRule"/> — "target opponent" (no
///   "or"), modelled as <see cref="ObjectReferenceKind.Opponent"/> rather than a
///   Target+Filter; "opponent"/"player" are therefore excluded from this rule's
///   type set so the two rules never compete for the same text.</item>
///   <item><see cref="SelfDealsDamageToAnyTargetTriggeredRule"/> — "any target",
///   a distinct <see cref="ObjectReferenceKind.AnyTarget"/> shape.</item>
/// </list>
///
/// Mirrors the activated-ability analogue's "to target &lt;type&gt;" branch in
/// <see cref="Activated.Rules.SelfDealsDamageToAnyTargetEffectRule"/>, which
/// handles the same shape for activated costs ("Sacrifice this creature: It deals
/// 2 damage to target creature.").
/// </summary>
[TriggeredRule]
public sealed class ItDealsDamageToTargetTypeTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^it\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
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
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [m.Groups["type"].Value.ToLowerInvariant()] },
      },
    };
    return true;
  }
}
