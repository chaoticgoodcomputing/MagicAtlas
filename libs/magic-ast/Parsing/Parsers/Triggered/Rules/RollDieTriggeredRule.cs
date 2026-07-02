namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Dice;

/// <summary>
/// "roll [count] [dN | N-sided die/dice]" — emits a <see cref="RollDieEffect"/> for the canonical
/// die-roll instruction (CR 706.1). Handles both the abbreviated "dN" form ("roll a d20") and the
/// spelled-out "N-sided die/dice" form ("roll a six-sided die", "roll two six-sided dice"), with an
/// optional die count (CR 706.1: "how many of those dice to roll").
///
/// <para>
/// CR 706.1a: the die has N equally likely outcomes numbered 1 to N. This rule handles rolls without a
/// results table (CR 706.4); the result is consumed by a following effect or by a die-roll trigger.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the roll phrase could appear as a substring of a longer results-table
/// instruction; anchoring prevents matching inside a more-specific sibling. Priority 60: above the
/// generic fallback (default 50) and below combat-damage specialisations (70+).
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class RollDieTriggeredRule : ITriggeredRule
{
  // "roll [count] dN" (abbreviated) OR "roll [count] N-sided die/dice" (spelled out), anchored.
  // Terminal period is stripped by the sentence-bundle dispatcher before dispatch.
  private static readonly Regex _pattern = new(
    @"^roll\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+"
      + @"(?:d(?<dn>\d+)|(?<sided>two|three|four|six|eight|ten|twelve|twenty|hundred)-sided\s+(?:die|dice))$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static int Word(string w) =>
    w.ToLowerInvariant() switch
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
      "twelve" => 12,
      "twenty" => 20,
      "hundred" => 100,
      _ => int.TryParse(w, out var n) ? n : 0,
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var count = Word(m.Groups["count"].Value);
    var sides = m.Groups["dn"].Success
      ? (int.TryParse(m.Groups["dn"].Value, out var n) ? n : 0)
      : Word(m.Groups["sided"].Value);
    if (count < 1 || sides < 2)
    {
      return false;
    }

    // null ≡ single die, so "roll a d20" serializes unchanged (no Count field).
    effect = new RollDieEffect { Sides = sides, Count = count == 1 ? null : count };
    return true;
  }
}
