namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "remove a +1/+1 counter from it" / "remove a charge counter from it" /
/// "remove a -1/-1 counter from this creature" as a triggered effect.
///
/// Covers counter-removal as the primary triggered action, e.g.:
///   "At the beginning of your upkeep, remove a -1/-1 counter from Aboroth."
///   "At end of combat, remove a +1/+1 counter from it."
///   "At the beginning of your upkeep, remove a charge counter from this artifact."
///
/// Counter type resolution mirrors <see cref="PutCountersTriggeredRule"/>:
///   "+1/+1" and "-1/-1" are matched directly; all other types are captured
///   from the "a(n) &lt;type&gt; counter" pattern (Rule 122.1 — named counters).
///
/// Target resolution order:
///   1. "from it"                                           → ObjectReference.It()
///   2. "from this creature/permanent/artifact/enchantment" → ObjectReference.Self()
///   3. Named self-reference fallback (card name in text)   → ObjectReference.Self()
/// </summary>
[TriggeredRule]
public sealed class RemoveCounterTriggeredRule : ITriggeredRule
{
  // Matches: "remove a(n)? <type> counter(s)? from <target-phrase>"
  // Handles both P/T counters (+1/+1, -1/-1) via inline checks and named counters
  // via the <type> capture group.
  private static readonly Regex Pattern = new(
    @"^remove\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>[\w\-/+]+)\s+counters?\s+from\s+(?<target>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    var count = rawCount switch
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
      _ => int.TryParse(rawCount, out var n) ? n : 1,
    };

    var rawType = m.Groups["type"].Value;

    // Counter type: keep +1/+1 and -1/-1 in canonical form; lowercase all others.
    string counterType;
    if (rawType == "+1/+1" || rawType == "-1/-1")
    {
      counterType = rawType;
    }
    else
    {
      counterType = rawType.ToLowerInvariant();
    }

    var targetPhrase = m.Groups["target"].Value.Trim().ToLowerInvariant();

    ObjectReference target;
    if (Regex.IsMatch(targetPhrase, @"^\bthis\s+(creature|permanent|artifact|enchantment|land)\b"))
    {
      target = ObjectReference.Self();
    }
    else if (targetPhrase == "it" || Regex.IsMatch(targetPhrase, @"^\bit\b"))
    {
      target = ObjectReference.It();
    }
    else
    {
      // Named self-reference: "from Aboroth", "from Phantom Nantuko", etc.
      // The card's own name appearing in oracle text always refers to self (Rule 201.4).
      target = ObjectReference.Self();
    }

    effect = new RemoveCountersEffect
    {
      Target = target,
      CounterType = counterType,
      Count = LiteralQuantity.Of(count),
    };
    return true;
  }
}
