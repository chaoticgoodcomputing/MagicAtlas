namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// Handles the triggered-ability effect shape
/// "target [filter] can't block this turn."
///
/// CR 603.6a: Enters-the-battlefield abilities trigger when a permanent enters
/// the battlefield. Written "When [this object] enters, ..." or
/// "Whenever a [type] enters, ..."
///
/// CR 509.1 (declare-blockers step): a "can't block" restriction applies
/// during the declare-blockers turn-based action. The effect is durative —
/// it expires at the end of the current turn (<c>untilEndOfTurn</c>).
/// </summary>
[TriggeredRule]
public sealed class CantBlockThisTurnTriggeredRule : ITriggeredRule
{
  // "target <filter> can't block this turn"
  private static readonly Regex Pattern = new(
    @"^target\s+(?<filter>.+?)\s+can'?t\s+block\s+this\s+turn$",
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

    var filterPhrase = m.Groups["filter"].Value.Trim();
    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
    {
      return false;
    }

    effect = new CantBlockEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
