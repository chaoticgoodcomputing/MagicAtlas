namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "target opponent discards [a/N] card[s]" as the effect clause of a triggered
/// ability — the Invasion of Eldraine ETB shape ("When this Siege enters, target
/// opponent discards two cards"). The discarder is a targeted opponent
/// (CR 115.1 — "target" in oracle text creates a targeting requirement); the
/// count is the literal quantity of cards moved from hand to graveyard
/// (CR 701.9a: "To discard a card, move it from its owner's hand to that player's
/// graveyard"). CR 603.2: the event-match (the Siege entering) is the trigger.
///
/// <para>
/// The spell-speed sibling for this surface is
/// <see cref="Spell.Rules.DiscardTargetPlayerRule"/> ("Target opponent discards
/// two cards." as a standalone spell effect); this rule is the triggered-side
/// counterpart, dispatched after the trigger/effect split has peeled the
/// "When this Siege enters," prefix. The all-opponents variant is
/// <see cref="EachOpponentDiscardsRule"/>; this rule is the single-targeted-opponent
/// shape, so the Player reference is <see cref="ObjectReferenceKind.Target"/>
/// filtered to "opponent" rather than <see cref="ObjectReferenceKind.EachOpponent"/>.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class TargetOpponentDiscardsTriggeredRule : ITriggeredRule
{
  // "target opponent discards a card" / "target opponent discards two cards" / etc.
  // The terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^target\s+opponent\s+discards?\s+(?<count>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?\.?$",
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

    var raw = m.Groups["count"].Value.ToLowerInvariant();
    int count = raw switch
    {
      "a" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.Parse(raw),
    };

    effect = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["opponent"] },
      },
    };
    return true;
  }
}
