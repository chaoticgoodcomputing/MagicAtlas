namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// "Until end of turn, you don't lose this mana as steps and phases end." —
/// the mana-persistence clause on Birgi, God of Storytelling.
///
/// <para>
/// CR 106.4 / CR 500.5: normally mana empties at the end of each step and phase.
/// This sentence is the oracle-text signal that the mana produced in the same
/// triggered ability persists until end of turn instead, overriding the turn-based
/// mana-emptying action.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the surface phrase "don't lose this mana as steps and phases end"
/// is highly specific and cannot match as a substring of another triggered rule.
/// Anchoring is maintained as a defensive convention (ADR anchor rule).
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ManaPersistsUntilEndOfTurnRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^Until\s+end\s+of\s+turn,\s+you\s+don't\s+lose\s+this\s+mana\s+as\s+steps\s+and\s+phases\s+end$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var t = text.Trim().TrimEnd('.').Trim();
    if (!_pattern.IsMatch(t))
    {
      return false;
    }

    effect = new ManaPersistsUntilEndOfTurnEffect
    {
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
