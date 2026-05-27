namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Draw [N] card(s) at the beginning of the next turn's upkeep."
///
/// Delayed draw shape: the draw effect carries an <see cref="AtBeginningOfNextUpkeepDuration"/>
/// to record that it resolves at the start of the next turn's upkeep step (Rule 313.1).
/// Covers literal and word counts (a, one, two …).
///
/// Examples: Jolt, Ray of Erasure, Force Void, Blessed Wine, Gravebind, Bone Harvest.
/// </summary>
[SpellRule(Priority = 60)]
public sealed class DrawCardsAtNextUpkeepRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Draw\s+(?<count>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?\s+at\s+the\s+beginning\s+of\s+the\s+next\s+turn's\s+upkeep$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    var count = rawCount switch
    {
      "a" or "one" => 1,
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

    effect = new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
      Duration = new AtBeginningOfNextUpkeepDuration(),
    };
    return true;
  }
}
