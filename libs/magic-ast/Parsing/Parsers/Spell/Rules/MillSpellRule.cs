namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Mill N cards." — spell-resolution self-mill keyword action. Rule 701.17.
/// Matches the standalone oracle clause form ("Mill two cards.", "Mill 4 cards.").
/// The implicit subject (controller) is encoded as Player = You.
/// For the triggered-ability side, see <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.MillTriggeredRule"/>.
/// </summary>
[SpellRule]
public sealed class MillSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Mill\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var token = m.Groups["count"].Value.ToLowerInvariant();
    var count = token switch
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

    effect = new MillEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
    };
    return true;
  }
}
