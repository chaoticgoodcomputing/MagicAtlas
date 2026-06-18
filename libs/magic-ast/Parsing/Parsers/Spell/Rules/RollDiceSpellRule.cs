namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Dice;

/// <summary>
/// "Roll [count] [dN | N-sided die/dice]" — the die-roll instruction (CR 706.1) in SPELL context
/// (instants/sorceries: "Roll two six-sided dice." — Pair o' Dice Lost). The triggered-ability path has
/// its own <c>RollDieTriggeredRule</c>; the spell-effect registry (ISpellRule) is separate, so the roll
/// needs a spell rule too. Emits the same <see cref="RollDieEffect"/> (Count nullable, null ≡ 1), which
/// projects <c>emit:rolldice</c> for the dice flow arm.
///
/// <para>Anchored (^…$). Priority 60 — above the generic fallback, mirrors the triggered roll rule.</para>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class RollDiceSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
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
    var m = Pattern.Match(text.Trim());
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
