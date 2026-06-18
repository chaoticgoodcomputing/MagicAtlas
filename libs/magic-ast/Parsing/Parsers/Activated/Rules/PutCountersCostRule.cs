namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put a -1/-1 counter on this creature" / "Put a +1/+1 counter on this permanent" —
/// a cost that places one or more counters on this permanent as part of activating an
/// ability (Devoted Druid: "Put a -1/-1 counter on this creature: Untap this creature.").
///
/// <para>
/// CR 122.1: "A counter is a marker placed on an object or player that modifies its
/// characteristics and/or interacts with a rule, ability, or effect."
/// CR 602.1a: "The activation cost is everything before the colon (:)."
/// Placing a counter is a valid activation cost; this rule is anchored
/// (^…$) to prevent matching an effect sentence that also contains "put" and "counter".
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 997)]
public sealed class PutCountersCostRule : IActivatedCostRule
{
  // Anchored: must be the whole cost component, not a substring of an effect sentence.
  // Captures: count word/number, counter type (+1/+1, -1/-1, or named), target noun.
  private static readonly Regex _pattern = new(
    @"^Put\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>\+1/\+1|-1/-1|[\w\-]+)\s+counters?\s+on\s+this\s+(?<noun>\w+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText)
  {
    var trimmed = costText.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    Quantity quantity = rawCount switch
    {
      "a" or "an" or "one" => LiteralQuantity.Of(1),
      "two" => LiteralQuantity.Of(2),
      "three" => LiteralQuantity.Of(3),
      "four" => LiteralQuantity.Of(4),
      "five" => LiteralQuantity.Of(5),
      "six" => LiteralQuantity.Of(6),
      "seven" => LiteralQuantity.Of(7),
      "eight" => LiteralQuantity.Of(8),
      "nine" => LiteralQuantity.Of(9),
      "ten" => LiteralQuantity.Of(10),
      _ => int.TryParse(rawCount, out var n) ? LiteralQuantity.Of(n) : LiteralQuantity.Of(1),
    };

    var counterType = m.Groups["type"].Value.ToLowerInvariant();

    return new PutCountersCost
    {
      CounterType = counterType,
      Quantity = quantity,
      Target = ObjectReference.Self(),
    };
  }
}
