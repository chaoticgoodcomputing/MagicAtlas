namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each [player|opponent] discards [a|N] card[s]" — the distributed discard
/// effect imposed on every player (or every opponent) simultaneously. Rule 701.9
/// (Discard). MAST records the discard event, the count, and the player scope
/// (<see cref="ObjectReferenceKind.EachPlayer"/> or
/// <see cref="ObjectReferenceKind.EachOpponent"/>); the simultaneous discard order
/// for multiplayer is engine territory, not described by the oracle text.
///
/// <para>
/// This is the triggered-side counterpart of the spell-level
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.DiscardEachPlayerRule"/> and
/// mirrors the scope/count shape of
/// <see cref="EachPlayerSacrificesTriggeredRule"/>: the player scope rides on the
/// <see cref="DiscardCardsEffect.Player"/> reference kind, and the count rides on
/// <see cref="DiscardCardsEffect.Count"/>. The dominant ETB form
/// ("When this creature enters, each player discards a card." — Miasmic Mummy,
/// Rotting Rats, Earsplitting Rats) lands here once the trigger/effect split has
/// peeled the trigger prefix. "a card" yields a literal count of 1.
/// </para>
///
/// <para>
/// Despite the legacy name, this rule covers both the <c>each player</c> and
/// <c>each opponent</c> scopes (the original opponent-only shape it was named for).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class EachOpponentDiscardsRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var scope = m.Groups["scope"].Value.ToLowerInvariant().Contains("opponent")
      ? ObjectReferenceKind.EachOpponent
      : ObjectReferenceKind.EachPlayer;

    var countLower = m.Groups["count"].Value.ToLowerInvariant();
    var n = countLower switch
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
      _ => int.TryParse(countLower, out var parsed) ? parsed : 0,
    };
    if (n <= 0)
    {
      return false;
    }

    effect = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(n),
      Player = new ObjectReference { Kind = scope },
    };
    return true;
  }

  // "each [player|opponent] discards [a|N] card[s][.]" — the distributed-discard
  // canonical form. Terminal period already stripped by the dispatcher before
  // matching; the optional "s" lets the count and noun number agree.
  private static readonly Regex _pattern = new(
    @"^each\s+(?<scope>player|opponent)\s+discards\s+(?<count>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
