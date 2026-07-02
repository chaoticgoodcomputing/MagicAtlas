namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that player loses N life" — symmetry-drain trigger on an opponent-controlled-ETB
/// trigger (Suture Priest pattern). "That player" is the pronoun for the player
/// whose action fired the trigger (Rule 109.5) and maps to
/// <see cref="ObjectReferenceKind.ThatPlayer"/>.
/// </summary>
[TriggeredRule]
public sealed class ThatPlayerLosesLifeRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
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
