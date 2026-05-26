namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you gain N life" — ETB lifegain trigger effect.
/// Rule 701.20: "To gain life, a player adds the indicated amount to their life total."
/// </summary>
[TriggeredRule]
public sealed class YouGainLifeRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^you\s+gain\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
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
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.Parse(raw),
    };
    effect = new GainLifeEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Player = ObjectReference.You(),
    };
    return true;
  }
}
