namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "look at the top N cards of your library, then put them back in any order." —
/// The Crystal Seer ETB pattern. Rule 701.12 (look) + unconditional reorder.
/// The controller looks at the top N cards then returns all of them in any chosen order.
/// Distinct from scry (top/bottom choice per card) and surveil (graveyard option).
/// </summary>
[TriggeredRule]
public sealed class LookAtTopCardsTriggeredRule : ITriggeredRule
{
  // Matches: "look at the top N cards of your library, then put them back in any order"
  // N can be a digit or a word number (two, three, four, five, etc.)
  private static readonly Regex _pattern = new(
    @"^look\s+at\s+the\s+top\s+(?<count>\d+|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+of\s+your\s+library,\s*then\s+put\s+them\s+back\s+in\s+any\s+order$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var countRaw = match.Groups["count"].Value.ToLowerInvariant();
    var count = ParseCount(countRaw);
    if (count is null)
    {
      return false;
    }

    effect = new LookAtCardsEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(count.Value),
      Zone = Zone.Library,
      Location = "Top",
      PutBackInAnyOrder = true,
    };
    return true;
  }

  private static int? ParseCount(string raw) =>
    raw switch
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
      _ when int.TryParse(raw, out var n) => n,
      _ => null,
    };
}
