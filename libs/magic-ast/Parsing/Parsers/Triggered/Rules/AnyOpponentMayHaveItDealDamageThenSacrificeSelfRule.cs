namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "any opponent may have it deal N damage to them. If a player does, sacrifice this
/// creature." — the opponent-choice downside ping (Longhorn Firebeast's ETB). An opponent
/// (not the controller) decides whether the source deals damage to that opponent; if one
/// does, the controller must sacrifice the source.
///
/// <para>
/// CR 118.12: "[A player] may [do something]. If [that player] [does…], [effect]." — the
/// whole clause is one <see cref="OptionalEffect"/> (wrapper presence IS the "may"; no bool,
/// ADR 0005). The decision-maker is NOT the controller, so <see cref="OptionalEffect.Chooser"/>
/// is set to <see cref="ObjectReferenceKind.Opponent"/> ("any opponent") rather than left null
/// (≡ "you may"); this is load-bearing because the source ("it") and the "sacrifice" actor
/// (the controller) are distinct from the choosing opponent, so the chooser cannot be inferred
/// from the inner references.
/// </para>
///
/// <para>
/// CR 120.1: an object (source) deals damage to a player. The source is the pronoun "it" — the
/// ability's own source permanent (mirrors <see cref="ItDealsDamageToTargetTypeDisjunctionRule"/>),
/// modeled as <see cref="ObjectReferenceKind.It"/>; absent any "combat damage" marker the damage
/// is non-combat, so <see cref="DealDamageEffect.IsCombat"/> is left null. The recipient "them"
/// is the choosing opponent (<see cref="ObjectReferenceKind.Opponent"/>).
/// </para>
///
/// <para>
/// The "If a player does" branch is the opt-in consequence, carried in
/// <see cref="OptionalEffect.IfYouDo"/> (the branch that fires when the chooser performs the
/// optional action). CR 701.21a: "To sacrifice a permanent, its controller moves it from the
/// battlefield directly to its owner's graveyard." — "sacrifice this creature" is a
/// <see cref="SacrificeEffect"/> whose target is the source itself
/// (<see cref="ObjectReferenceKind.Self"/>). CR 603.2: triggered abilities.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the full two-sentence effect interior is matched, so no sibling
/// "sacrifice this creature" / "deal N damage" surface in a different pattern is mis-labelled.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class AnyOpponentMayHaveItDealDamageThenSacrificeSelfRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^any\s+opponent\s+may\s+have\s+it\s+deals?\s+"
    + @"(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+them\.\s+"
    + @"If\s+a\s+player\s+does,\s+sacrifice\s+this\s+creature\.?$",
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

    effect = new OptionalEffect
    {
      Chooser = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
      Inner = new DealDamageEffect
      {
        Amount = LiteralQuantity.Of(amount),
        Source = ObjectReference.It(),
        Target = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
      },
      IfYouDo = new SacrificeEffect
      {
        Target = ObjectReference.Self(),
      },
    };
    return true;
  }
}
