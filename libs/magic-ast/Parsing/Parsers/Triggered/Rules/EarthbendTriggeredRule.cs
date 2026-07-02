namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;

/// <summary>
/// "earthbend N" — the Earthbend keyword action as a triggered effect.
///
/// <para>
/// CR 701.66a (verbatim): "\"Earthbend N\" means \"Target land you control becomes
/// a 0/0 land creature with haste in addition to its other types. Put N +1/+1 counters
/// on it. When that land dies or is put into exile, return it to the battlefield tapped
/// under your control.\""
/// </para>
///
/// <para>
/// MAST records the keyword-action and its N parameter descriptively. The animation,
/// counter placement, and delayed triggered ability are engine territory (CR 701.66b).
/// </para>
///
/// <para>
/// Pattern is anchored (^...$) to prevent matching as a substring of a more-specific
/// sibling. Handles an optional leading "then " prefix so the rule fires correctly
/// when the sentence-bundle splitter produces a "then earthbend" fragment.
/// </para>
/// </summary>
[TriggeredRule(Priority = 72)]
public sealed class EarthbendTriggeredRule : ITriggeredRule
{
  // Anchored. Optional "then " prefix (sentence-bundle splitter may produce it).
  private static readonly Regex _pattern = new(
    @"^(?:then\s+)?earthbend\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = rawAmount switch
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
      _ => int.Parse(rawAmount),
    };

    effect = new EarthbendEffect { Count = LiteralQuantity.Of(amount) };
    return true;
  }
}
