namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this artifact deals N damage to it" — the self-ping resolution of the
/// "whenever a creature attacks/blocks/enters/etc." family whose trigger
/// condition names the affected object with an INDEFINITE pronoun ("a
/// creature") rather than a definite one ("that creature"). Covers Caltrops:
/// "Whenever a creature attacks, this artifact deals 1 damage to it."
///
/// <para>
/// The source is the permanent itself (CR 109 — "this artifact" is a
/// self-reference), modelled as <see cref="ObjectReference.Self"/>. The
/// target is "it" — the anaphoric pronoun (CR 113.8b) back-referring to the
/// object named by this ability's own trigger event (the attacking
/// creature), modelled as <see cref="ObjectReferenceKind.It"/>. This mirrors
/// the established "it" convention for trigger-named back-references (see
/// <c>YouMayCreateTokenCopyOfItTriggeredRule</c>), and is distinct from
/// <see cref="ThisCreatureDealsDamageToThatCreatureTriggeredRule"/>, whose
/// trigger condition instead names the object definitely ("that creature")
/// via <see cref="ObjectReferenceKind.ThatCreature"/>.
/// </para>
///
/// <para>
/// Rule 120.1–120.2: dealing damage (a source deals damage to a permanent or
/// player). Rule 603.2: triggered abilities (When/Whenever/At). The dealt
/// damage is NONCOMBAT damage (CR 120): a triggered ability resolving and
/// dealing damage is not itself combat damage assigned during the combat
/// damage step (CR 510), so <see cref="DealDamageEffect.IsCombat"/> is left
/// null (≡ noncombat) and omitted from the JSON — mirrors the sibling rule's
/// IsCombat reasoning.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThisCreatureDealsDamageToItTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^this\s+(?:creature|permanent|artifact|enchantment)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+it\.?$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
    };
    return true;
  }
}
