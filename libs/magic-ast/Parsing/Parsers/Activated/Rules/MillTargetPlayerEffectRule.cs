namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Activated-ability mill keyword action. CR 701.17a:
/// "For a player to mill a number of cards, that player puts that many cards
/// from the top of their library into their graveyard."
///
/// Handles the targeted-player form used by activated abilities:
///   "Target player mills N cards."
///   "Target opponent mills N cards."
///
/// For the spell-resolution side see <c>MillSpellRule</c>.
/// For the triggered-ability side see <c>MillTriggeredRule</c>.
/// </summary>
[ActivatedEffectRule(Priority = 996)]
public sealed class MillTargetPlayerEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+(?<target>player|opponent)\s+mills?\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var match = Pattern.Match(effectText.Trim());
    if (!match.Success)
    {
      return null;
    }

    var isOpponent = match.Groups["target"].Value.Equals("opponent", StringComparison.OrdinalIgnoreCase);
    var player = isOpponent
      ? new ObjectReference { Kind = ObjectReferenceKind.Opponent }
      : ObjectReference.Target(ObjectFilter.Player());

    return new MillEffect
    {
      Count = LiteralQuantity.Of(ParseCount(match.Groups["count"].Value)),
      Player = player,
    };
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
      var t => int.TryParse(t, out var n) ? n : 1,
    };
}
