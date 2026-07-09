namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "defending player discards [a/N] card[s]" (optionally "at random") as the effect
/// clause of a triggered ability — the combat-nuisance shape on attack/block triggers
/// (Alley Grifters, Slate Street Ruffian: "Whenever this creature becomes blocked,
/// defending player discards a card"; Shrieking Specter / The Haunt of Hightower on
/// attack; Corrupt Official's "…discards a card at random").
///
/// <para>
/// The discarder is <see cref="ObjectReferenceKind.DefendingPlayer"/> — CR 508.5:
/// "If an ability of an attacking creature refers to a defending player … the defending
/// player it's referring to is the player that creature is attacking …" (equivalently,
/// on a becomes-blocked trigger, the player who declared the block). The card count is
/// the literal quantity of cards moved from hand to graveyard — glossary "Discard": "To
/// move a card from its owner's hand to that player's graveyard. See rule 701.9,
/// 'Discard.'" A trailing "at random" sets <see cref="DiscardCardsEffect.Random"/>.
/// </para>
///
/// <para>
/// This is the defending-player counterpart of
/// <see cref="TargetOpponentDiscardsTriggeredRule"/> ("target opponent discards …");
/// both are dispatched after the trigger/effect split has peeled the "Whenever …,"
/// prefix, and both emit <see cref="DiscardCardsEffect"/> differing only in the
/// <see cref="DiscardCardsEffect.Player"/> reference kind. The surface is anchored
/// (<c>^…$</c>) so the "at random" and "…all the cards in their hand, then draws that
/// many cards" (Robber Fly) siblings are NOT mislabeled — the former is captured
/// explicitly by the optional group, the latter falls through unmatched.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class DefendingPlayerDiscardsTriggeredRule : ITriggeredRule
{
  // The terminal period is stripped by the dispatcher before TryMatch is called; the
  // optional "\.?" tolerates callers that leave it. Anchored end-to-end.
  private static readonly Regex _pattern = new(
    @"^defending\s+player\s+discards?\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?(?<random>\s+at\s+random)?\.?$",
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
      "a" or "an" or "one" => 1,
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
      Player = new ObjectReference { Kind = ObjectReferenceKind.DefendingPlayer },
      Random = m.Groups["random"].Success,
    };
    return true;
  }
}
