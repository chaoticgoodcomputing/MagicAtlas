namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this creature deals N damage to that creature" — the self-ping resolution of
/// the "blocks-or-becomes-blocked" combat-trigger family (Inferno Elemental,
/// Skewer Slinger, Ornery Goblin, …). The source is the creature itself
/// (CR 109 — "this creature" is a self-reference); the target is the creature
/// named by the trigger condition (the blocking/blocked creature), encoded as
/// <see cref="ObjectReferenceKind.ThatCreature"/> — the creature analogue of
/// "that player" (mirrors Flanking's "the blocking creature" target).
///
/// <para>
/// The dealt damage is NONCOMBAT damage (CR 120): this is a triggered ability
/// resolving and dealing damage, not combat damage assigned during the combat
/// damage step (CR 510). The trigger FIRES off combat (a block declaration), but
/// the damage the ability deals is not itself combat damage, so
/// <see cref="DealDamageEffect.IsCombat"/> is left null (≡ noncombat) and omitted
/// from the JSON. This distinction is load-bearing for the interaction layer: the
/// emit must not feed a "deals <em>combat</em> damage" trigger (CR 510), but does
/// feed a bare "deals damage" trigger (CR 120) — see the IsCombat doc-comment on
/// <see cref="DealDamageEffect"/>.
/// </para>
///
/// <para>
/// Rule 120.1–120.2: dealing damage (a source deals damage to a permanent or
/// player). Rule 603.2: triggered abilities (When/Whenever/At). Generalises over
/// the type word ("creature"/"permanent"/…) on both the source pronoun and the
/// "that [type]" target so the whole family is covered, not a single exemplar.
/// </para>
///
/// <para>
/// Distinct from <see cref="ThisCreatureDealsDamageToAnyTargetTriggeredRule"/>
/// (target = <see cref="ObjectReferenceKind.AnyTarget"/>): here the target is the
/// trigger-named back-reference <see cref="ObjectReferenceKind.ThatCreature"/>,
/// not a freely-chosen "any target".
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThisCreatureDealsDamageToThatCreatureTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^this\s+(?:creature|permanent|artifact|enchantment)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+that\s+(?:creature|permanent|artifact|enchantment)\.?$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
    };
    return true;
  }
}
