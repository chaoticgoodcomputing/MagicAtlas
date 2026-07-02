namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "they lose N life" — triggered life-loss where "they" is the player who
/// performed the triggering action (CR 119.3: if an effect causes a player to
/// lose life, that player's life total is adjusted accordingly; CR 603.2: the
/// trigger fires whenever the event matches).
/// "They" is a pronoun back-reference to the player identified by the trigger's
/// filter (e.g. "an opponent draws a card, they lose N life") — that opponent is
/// <see cref="ObjectReferenceKind.ThatPlayer"/>.
/// </summary>
[TriggeredRule]
public sealed class TheyLoseLifeRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^they\s+lose\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
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
      Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
    };
    return true;
  }
}
