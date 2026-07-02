namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals N damage to target [type] or [type]." — the ETB-damage family with a
/// <b>disjunctive target</b>. Covers cards whose triggered ETB ability reads
/// "When this creature enters, it deals N damage to target player or planeswalker"
/// (Viashino Pyromancer, Keldon Champion, Goretusk Firebeast, Sparkcaster) and
/// "… target opponent or planeswalker" (Cinder Hellion, Gruesome Scourger).
///
/// Rule 603: triggered abilities (When/Whenever/At). Rule 120.1: a source deals
/// damage to a creature, planeswalker, battle, or player; absent any "combat damage"
/// marker the damage is non-combat (Rule 120 vs Rule 510), so <see cref="DealDamageEffect.IsCombat"/>
/// is left null. Rule 115.1: "target" creates a target. The disjunction "X or Y" is a
/// single target whose set of legal types is the union {X, Y} (Rule 115.4 reasoning
/// generalized to an explicit two-type union), so it is one <see cref="ObjectReferenceKind.Target"/>
/// with a <see cref="ObjectFilter.CardTypes"/> union — the same shape the spell-side
/// <see cref="Spell.Rules.SelfDealsDamageToTypeDisjunctionRule"/> produces for Lava Axe.
/// "player"/"opponent" are listed alongside card types because <see cref="ObjectFilter.CardTypes"/>
/// encodes both card types and game entities that can be dealt damage (Rule 120.1);
/// "opponent" additionally carries the Rule 102.2 relation (a player not on the
/// controller's team).
///
/// The "or" is REQUIRED: the bare single-type forms are handled by their dedicated
/// rules (<see cref="SelfDealsDamageToOpponentRule"/> for "target opponent",
/// <see cref="SelfDealsDamageToAnyTargetTriggeredRule"/> for "any target"), so this
/// rule fires only on the two-type disjunction and never double-matches them.
/// The subject is the triggered pronoun "it" — the source of the ability ("some
/// abilities cause a source to do something … 'This creature deals 1 damage to any
/// target'"), modeled as <see cref="ObjectReferenceKind.It"/>.
/// </summary>
[TriggeredRule]
public sealed class ItDealsDamageToTargetTypeDisjunctionRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^it\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+target\s+(?<type1>player|opponent|creature|artifact|enchantment|land|planeswalker|permanent)\s+or\s+(?<type2>player|opponent|creature|artifact|enchantment|land|planeswalker|permanent)\.?$",
      RegexOptions.IgnoreCase
    );
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

    effect = new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.It(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = cardTypes },
      },
    };
    return true;
  }
}
