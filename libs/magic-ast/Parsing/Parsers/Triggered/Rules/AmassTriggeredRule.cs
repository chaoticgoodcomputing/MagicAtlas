namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;

/// <summary>
/// "amass [Subtype] N" / "amass N" — the Amass keyword action as a triggered effect.
///
/// <para>
/// CR 701.47a: "Amass [subtype] N means 'Either put N +1/+1 counters on an Army
/// you control, or create a 0/0 black [Subtype] Army creature token, then put N
/// +1/+1 counters on it.'"
/// </para>
///
/// <para>
/// Matches both the modern typed form ("amass Orcs 1", which specifies a subtype)
/// and the legacy untyped form ("amass 1", from War of the Spark before subtype-
/// specific amass was templated). Handles an optional leading "Then " prefix (from
/// multi-sentence oracle text like "...deals 1 damage. Then amass Orcs 1.") so the
/// rule fires correctly when the sentence-bundle splitter produces a "Then amass"
/// fragment.
/// </para>
///
/// <para>
/// MAST records the keyword-action and its parameters descriptively. The army-
/// selection logic, token creation, and counter placement are engine territory.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class AmassTriggeredRule : ITriggeredRule
{
  // Matches: optional "Then " prefix, "amass [OptionalSubtype] N"
  // Subtype: an optional capitalized word (e.g. "Orcs") preceding the count.
  private static readonly Regex _pattern = new(
    @"^(?:then\s+)?amass\s+(?:(?<subtype>[A-Z][A-Za-z]*)\s+)?(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\.?$",
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
      "one" => 1, "two" => 2, "three" => 3, "four" => 4, "five" => 5,
      "six" => 6, "seven" => 7, "eight" => 8, "nine" => 9, "ten" => 10,
      _ => int.Parse(rawAmount),
    };

    // Extract optional subtype (e.g., "Orcs" from "amass Orcs 1").
    string? subtype = null;
    if (m.Groups["subtype"].Success && !string.IsNullOrEmpty(m.Groups["subtype"].Value))
    {
      subtype = m.Groups["subtype"].Value;
    }

    effect = new AmassEffect
    {
      Count = LiteralQuantity.Of(amount),
      ArmySubtype = subtype,
    };
    return true;
  }
}
