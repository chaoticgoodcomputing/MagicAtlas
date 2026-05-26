namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "mill N cards." / "mill a card." — Rule 701.17 keyword action on the triggered side.
/// The implicit subject (controller) is encoded as Player = You.
/// </summary>
[TriggeredRule]
public sealed class MillTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var match = Regex.Match(
      trimmed,
      @"^mill\s+(a|an|one|two|three|four|five|\d+)\s+cards?$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return false;
    }
    var countToken = match.Groups[1].Value.ToLowerInvariant();
    var count = countToken switch
    {
      "a" or "an" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.TryParse(countToken, out var n) ? n : 1,
    };
    effect = new MillEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
    };
    return true;
  }
}
