namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;

/// <summary>
/// "incubate N" — the Incubate keyword action as a triggered effect (CR 701.53).
///
/// <para>
/// CR 701.53a: "To incubate N, create an Incubator token that enters the
/// battlefield with N +1/+1 counters on it. See rule 111.10i."
/// </para>
///
/// <para>
/// CR 603.6a: Enters-the-battlefield abilities trigger when a permanent enters the
/// battlefield. These are written, "When [this object] enters, . . ." — Converter
/// Beast's "When this creature enters, incubate 5." is such an ETB trigger.
/// </para>
///
/// <para>
/// The rule receives the effect text AFTER the parser has split off the trigger
/// condition and stripped the trailing "(…)" reminder (CR 207.2), so it sees exactly
/// "incubate 5". MAST records the keyword-action and its count descriptively; the
/// Incubator token, its counters, and its "{2}: Transform this token." body are
/// engine territory (reminder text, CR 701.53b). Emits the bare
/// <see cref="IncubateEffect"/> — incubate is mandatory, not a "may".
/// </para>
/// </summary>
[TriggeredRule]
public sealed class IncubateTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^incubate\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\.?$",
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

    effect = new IncubateEffect
    {
      Count = LiteralQuantity.Of(amount),
    };
    return true;
  }
}
