namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Remove-counters cost: "Remove three spore counters from this creature" /
/// "Remove two charge counters from this permanent" (Rule 122). The counter type
/// is a named counter; the source is <see cref="ObjectReference.Self()"/> since
/// oracle text always reads "from this creature / permanent".
///
/// <para>Beyond the literal counts, scaling-mana costs (ADR 0009) also match
/// "Remove X [type] counters from this [noun]" → <c>VariableQuantity.X</c>
/// (CR 107.3) and "Remove any number of [type] counters from this [noun]" →
/// <see cref="AnyAmountQuantity"/>.</para>
/// </summary>
[ActivatedCostRule(Priority = 996)]
public sealed class RemoveCountersCostRule : IActivatedCostRule
{
  public Cost? TryMatch(string costText)
  {
    var trimmed = costText.Trim();
    // Pattern: "Remove <count> <type> counter(s) from this <noun>", where
    // <count> is a literal, "X", or "any number of".
    var m = Regex.Match(
      trimmed,
      @"^Remove\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+|X|any\s+number\s+of)\s+(?<type>[\w\-/+]+)\s+counters?\s+from\s+this\s+\w+$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    // Collapse any internal whitespace in "any number of" for the switch.
    var normalizedCount = Regex.Replace(rawCount, @"\s+", " ");
    Quantity? quantity = normalizedCount switch
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
      "x" => VariableQuantity.X,
      "any number of" => new AnyAmountQuantity(),
      _ => int.TryParse(normalizedCount, out var n) ? LiteralQuantity.Of(n) : null,
    };
    if (quantity is null)
    {
      return null;
    }

    var counterType = m.Groups["type"].Value.ToLowerInvariant();

    return new RemoveCountersCost
    {
      CounterType = counterType,
      Quantity = quantity,
      Target = ObjectReference.Self(),
    };
  }
}
