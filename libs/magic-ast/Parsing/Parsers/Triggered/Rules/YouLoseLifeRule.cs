namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you lose N life" — Deadpool's upkeep tax shape.
/// </summary>
[TriggeredRule]
public sealed class YouLoseLifeRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^you\s+lose\s+(?<amount>\d+|one|two|three|four|five)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(raw),
    };
    effect = new LoseLifeEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Player = ObjectReference.You(),
    };
    return true;
  }
}
