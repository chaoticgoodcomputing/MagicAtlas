namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "mill N cards." / "mill a card." — Rule 701.17 keyword action on the triggered side.
/// The implicit subject (controller) is encoded as Player = You.
/// Also handles "target player mills N cards." (Constellation pattern such as
/// Sage of Mysteries / Thassa's Devourer) where the target is encoded as
/// Player = Target + player filter (Rule 115.1).
/// </summary>
[TriggeredRule]
public sealed class MillTriggeredRule : ITriggeredRule
{
  private static readonly Regex SelfPattern = new(
    @"^mill\s+(a|an|one|two|three|four|five|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex TargetPattern = new(
    @"^target\s+(?<target>player|opponent)\s+mills?\s+(?<count>a|an|one|two|three|four|five|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    var selfMatch = SelfPattern.Match(trimmed);
    if (selfMatch.Success)
    {
      effect = new MillEffect
      {
        Count = LiteralQuantity.Of(ParseCount(selfMatch.Groups[1].Value)),
        Player = ObjectReference.You(),
      };
      return true;
    }

    var targetMatch = TargetPattern.Match(trimmed);
    if (targetMatch.Success)
    {
      var isOpponent = targetMatch.Groups["target"].Value.Equals(
        "opponent",
        StringComparison.OrdinalIgnoreCase
      );
      var player = isOpponent
        ? new ObjectReference { Kind = ObjectReferenceKind.Opponent }
        : ObjectReference.Target(ObjectFilter.Player());
      effect = new MillEffect
      {
        Count = LiteralQuantity.Of(ParseCount(targetMatch.Groups["count"].Value)),
        Player = player,
      };
      return true;
    }

    return false;
  }

  private static int ParseCount(string token) =>
    token.ToLowerInvariant() switch
    {
      "a" or "an" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.TryParse(token, out var n) ? n : 1,
    };
}
