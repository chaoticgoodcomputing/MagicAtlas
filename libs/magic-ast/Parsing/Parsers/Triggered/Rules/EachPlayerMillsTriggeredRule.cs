namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each player mills N cards." / "each opponent mills N cards." — Rule 701.17
/// keyword action (mill) broadcast across every player or every opponent
/// simultaneously, on the triggered-ability effect side.
///
/// <para>
/// CR 701.17a (verbatim): "For a player to mill a number of cards, that player
/// puts that many cards from the top of their library into their graveyard."
/// </para>
///
/// <para>
/// Mirrors <c>EachPlayerMillsNCardsEffectRule</c> (the activated-ability sibling
/// covering "{cost}: Each player mills N cards.") but on the triggered side, e.g.
/// Returned Reveler: "When this creature dies, each player mills three cards."
/// Anchored (^ … $) so it never collides with the self-mill or targeted-mill
/// patterns handled by <see cref="MillTriggeredRule"/>, both of which require the
/// clause to start with "mill"/"target", not "each player"/"each opponent".
/// </para>
/// </summary>
[TriggeredRule]
public sealed class EachPlayerMillsTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^each\s+(?<scope>player|opponent)\s+mills?\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var scope = m.Groups["scope"].Value.ToLowerInvariant();
    var kind = scope == "opponent" ? ObjectReferenceKind.EachOpponent : ObjectReferenceKind.EachPlayer;

    effect = new MillEffect
    {
      Count = LiteralQuantity.Of(ParseCount(m.Groups["count"].Value)),
      Player = new ObjectReference { Kind = kind },
    };
    return true;
  }

  private static int ParseCount(string token) =>
    token.ToLowerInvariant() switch
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
      _ => int.TryParse(token, out var n) ? n : 1,
    };
}
