namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[This creature|CardName] deals N damage to target opponent or planeswalker."
/// — the activated-ability sibling of
/// <see cref="SelfDealsDamageToPlayerOrPlaneswalkerEffectRule"/> for the
/// <c>opponent</c> variant (Zealot of the God-Pharaoh's
/// "{4}{R}: This creature deals 2 damage to target opponent or planeswalker.").
/// The source is the ability's own object, modeled as
/// <see cref="ObjectReference.Self()"/> (CR 602.1 — an activated ability is written
/// "[Cost]: [Effect]"; here the effect's source is "this creature").
///
/// The disjunctive target "opponent or planeswalker" is a single
/// <see cref="ObjectReferenceKind.Target"/> whose <see cref="ObjectFilter.CardTypes"/>
/// carries the two-entry union <c>["opponent", "planeswalker"]</c> — the same shape
/// the triggered sibling
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.ItDealsDamageToTargetTypeDisjunctionRule"/>
/// produces for the ETB variants (Cinder Hellion). Both "opponent" (a player, CR 102.2:
/// "a player's opponent is the other player") and "planeswalker" are legal recipients of
/// damage (CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players."). CR 115.1: "target" creates a target.
///
/// Anchored (<c>^…$</c>) and REQUIRES "opponent or planeswalker" exactly, so it stays
/// disjoint from the "player or planeswalker" sibling and the "each opponent" / "any
/// target" / single-type rules — no double-match.
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class SelfDealsDamageToOpponentOrPlaneswalkerEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Regex.Match(
      trimmed,
      @"^(?<subject>This\s+creature|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+target\s+opponent\s+or\s+planeswalker$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    // Accept "This creature" (pronoun self-ref) or a capitalised proper-noun subject.
    var subject = m.Groups["subject"].Value;
    var isThisCreature = subject.Equals("This creature", StringComparison.OrdinalIgnoreCase);
    var isNamedSelf = subject.Length > 0 && char.IsUpper(subject[0]) && !isThisCreature;
    if (!isThisCreature && !isNamedSelf)
    {
      return null;
    }

    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = rawAmount switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(rawAmount),
    };

    return new MagicAST.AST.Effects.Damage.DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["opponent", "planeswalker"] },
      },
    };
  }
}
