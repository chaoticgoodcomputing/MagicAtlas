namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Draw [N] card(s)." and "You draw [N] card(s)." — imperative and
/// player-subject forms of spell-resolution card draw (Rule 120.1).
/// The "You draw" form appears as the second sentence in multi-effect spells
/// like Crushing Disappointment ("Each player loses 2 life. You draw two cards.").
/// </summary>
[SpellRule]
public sealed class DrawCardsSimpleRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^(?:You\s+)?[Dd]raw\s+(?<count>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+|X|Y|Z)\s+cards?$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    var raw = m.Groups["count"].Value;
    var rawLower = raw.ToLowerInvariant();
    Quantity count;
    if (rawLower is "x" or "y" or "z")
    {
      count = new VariableQuantity { Name = rawLower.ToUpperInvariant() };
    }
    else
    {
      int n;
      if (rawLower == "a" || rawLower == "one")
      {
        n = 1;
      }
      else if (int.TryParse(rawLower, out var asDigit))
      {
        n = asDigit;
      }
      else
      {
        n = rawLower switch
        {
          "two" => 2,
          "three" => 3,
          "four" => 4,
          "five" => 5,
          "six" => 6,
          "seven" => 7,
          "eight" => 8,
          "nine" => 9,
          "ten" => 10,
          _ => 1,
        };
      }
      count = LiteralQuantity.Of(n);
    }
    effect = new DrawCardsEffect
    {
      Count = count,
      Player = ObjectReference.You(),
    };
    return true;
  }
}
