namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Dice;

/// <summary>
/// "Roll [count] [dN | N-sided die/dice]" — the die-roll instruction (CR 706.1) as the effect of an
/// ACTIVATED ability ("{2}{G}: Roll a six-sided die." — Willing Test Subject, Atomwheel Acrobats).
/// These are repeatable roll OUTLETS: an activated ability whose cost the controller pays to produce a
/// die-roll event, which a "whenever you roll" trigger (<c>DiceRolledConditionRule</c>) can then consume.
///
/// <para>
/// The triggered path (<c>RollDieTriggeredRule</c>) and the spell path (<c>RollDiceSpellRule</c>) each
/// have their own roll rule because their rule registries are distinct (ITriggeredRule / ISpellRule /
/// IActivatedEffectRule). This is the activated registry's roll rule. All three emit the same
/// <see cref="RollDieEffect"/> node (Count nullable, null ≡ the single-die form), which the interaction
/// projection turns into <c>emit:rolldice</c>.
/// </para>
///
/// <para>
/// The cost ("{2}{G}", "{4}, Sacrifice this artifact") is parsed separately by the activated cost rules;
/// this rule only recognises the post-colon effect fragment. Handles both the abbreviated "dN" form
/// ("Roll a d20") and the spelled-out "N-sided die/dice" form ("Roll a six-sided die"), with an optional
/// die count (CR 706.1: "how many of those dice to roll"), generalising over die size.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): a bare roll instruction with no trailing result-handling. A roll bundled with
/// inline result-handling ("Roll a d6. Until end of turn, …" — Ebony Fly) is a different shape that the
/// anchor deliberately does not match, leaving it for a future result-table-aware rule rather than
/// silently dropping the result clause. Priority 60 mirrors the triggered/spell roll rules — above the
/// generic fallback (default 50).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 60)]
public sealed class RollDieActivatedRule : IActivatedEffectRule
{
  // "Roll [count] dN" (abbreviated) OR "Roll [count] N-sided die/dice" (spelled out), anchored.
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

  public Effect? TryMatch(string effectText)
  {
    var m = _pattern.Match(effectText.Trim().TrimEnd('.').Trim());
    if (!m.Success)
    {
      return null;
    }

    var count = Word(m.Groups["count"].Value);
    var sides = m.Groups["dn"].Success
      ? (int.TryParse(m.Groups["dn"].Value, out var n) ? n : 0)
      : Word(m.Groups["sided"].Value);
    if (count < 1 || sides < 2)
    {
      return null;
    }

    // null ≡ single die, so "Roll a d20" serializes unchanged (no Count field).
    return new RollDieEffect { Sides = sides, Count = count == 1 ? null : count };
  }
}
