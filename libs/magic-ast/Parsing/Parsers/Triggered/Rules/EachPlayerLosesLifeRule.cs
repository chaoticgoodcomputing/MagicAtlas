namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each player loses N life" — a symmetric life-loss effect (CR 119.3,
/// VERBATIM: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly."). "Each player" scopes the
/// effect to every player in the game (controller and opponents alike) and
/// maps to <see cref="ObjectReferenceKind.EachPlayer"/>. Commonly paired with
/// an enters-the-battlefield trigger (CR 603.6) such as Howling Banshee's
/// "When this creature enters, each player loses 3 life."
/// </summary>
[TriggeredRule]
public sealed class EachPlayerLosesLifeRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^each\s+player\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
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
    effect = new LoseLifeEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
    };
    return true;
  }
}
