namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Dice;

/// <summary>
/// "roll a dN" — emits a <see cref="RollDieEffect"/> for the canonical die-roll
/// instruction (CR 706.1).
///
/// <para>
/// CR 706.1: "An effect that instructs a player to roll a die will specify what
/// kind of die to roll and how many of those dice to roll." CR 706.1a: the die
/// has N equally likely outcomes numbered 1 to N. This rule handles the single-die
/// form ("roll a d20") without a results table (CR 706.4).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): "roll a d20" is a short phrase that could appear as a
/// substring of a longer instruction (e.g. a results-table card that also says
/// "roll a d20 and consult …"). Anchoring prevents this rule from matching inside
/// a more-specific sibling that the corpus would need its own rule for.
/// Priority 60: above the generic fallback (default 50) and below other combat-
/// damage specialisations (70+).
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class RollDieTriggeredRule : ITriggeredRule
{
  // "roll a dN" where N is a positive integer — anchored to the full sentence.
  // Terminal period is stripped by the sentence-bundle dispatcher before dispatch.
  private static readonly Regex _pattern = new(
    @"^roll\s+a\s+d(?<sides>\d+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    if (!int.TryParse(m.Groups["sides"].Value, out var sides) || sides < 2)
    {
      return false;
    }

    effect = new RollDieEffect { Sides = sides };
    return true;
  }
}
