namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may have this creature deal N damage to target [type] or [type]." — the
/// OPTIONAL self-ping family with a <b>disjunctive target</b>. Covers triggered
/// abilities whose resolution reads "… you may have this creature deal 1 damage to
/// target player or planeswalker" (Hissing Iguanar, Cosi's Ravager, Kyren Sniper,
/// Exuberant Firestoker, Noggle Hedge-Mage — the disjunction differs only in the
/// controlling trigger/intervening-if, which the parser peels before this rule sees
/// the effect interior).
///
/// <para>
/// CR 118.12: "[A player] may [do something] …" — the "you may" makes the ping an
/// optional action taken when the ability resolves, so the whole effect is wrapped in
/// an <see cref="OptionalEffect"/> (the wrapper presence IS the "you may"; no bool —
/// ADR 0005). Its <see cref="OptionalEffect.Inner"/> is the <see cref="DealDamageEffect"/>.
/// </para>
///
/// <para>
/// CR 120.1: a source deals damage to a creature, planeswalker, battle, or player;
/// absent any "combat damage" marker the damage is non-combat (CR 120 vs CR 510), so
/// <see cref="DealDamageEffect.IsCombat"/> is left null. The source is "this creature"
/// — the permanent itself (CR 109), modeled as <see cref="ObjectReference.Self"/> (the
/// same pronoun shape <see cref="ThisCreatureDealsDamageToAnyTargetTriggeredRule"/>
/// produces). CR 115.1: "target" creates a target; the disjunction "X or Y" is one
/// target whose legal-type set is the union {X, Y} (the same shape
/// <see cref="ItDealsDamageToTargetTypeDisjunctionRule"/> produces for the mandatory
/// "it deals … to target player or planeswalker" ETB form), so it is one
/// <see cref="ObjectReferenceKind.Target"/> with an <see cref="ObjectFilter.CardTypes"/>
/// union — "player"/"opponent" listed alongside card types because CardTypes encodes
/// both card types and the game entities that can be dealt damage (CR 120.1).
/// </para>
///
/// <para>
/// The "or" is REQUIRED (the anchored <c>… or …</c> alternation): the bare single-type
/// forms ("… deal N damage to target creature") are a distinct family and are not
/// matched here, so this rule never shadows them. CR 603.2: triggered abilities.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class MayHaveThisCreatureDealDamageToTargetTypeDisjunctionRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+have\s+this\s+(?:creature|permanent)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+target\s+(?<type1>player|opponent|creature|artifact|enchantment|land|planeswalker|permanent)\s+or\s+(?<type2>player|opponent|creature|artifact|enchantment|land|planeswalker|permanent)\.?$",
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

    var cardTypes = new List<string>
    {
      m.Groups["type1"].Value.ToLowerInvariant(),
      m.Groups["type2"].Value.ToLowerInvariant(),
    };

    effect = new OptionalEffect
    {
      Inner = new DealDamageEffect
      {
        Amount = LiteralQuantity.Of(amount),
        Source = ObjectReference.Self(),
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = cardTypes },
        },
      },
    };
    return true;
  }
}
