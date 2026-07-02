namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// Recognises fight-spell oracle text in the form:
/// <list type="bullet">
///   <item>"Target creature you control fights target creature you don't control."</item>
///   <item>"Target creature you control fights target creature an opponent controls."</item>
/// </list>
///
/// CR 701.14 (Fight keyword action). Reminder text
/// "(Each deals damage equal to its power to the other.)" is stripped by the
/// parenthetical-removal pass before spell rules run, so it does not appear in
/// the input text here.
/// </summary>
[SpellRule]
public sealed class FightRule : ISpellRule
{
  // Matches:
  //   "Target creature you control fights target creature you don't control"
  //   "Target creature you control fights target creature an opponent controls"
  // Both produce identical AST (Controller: Opponent on the second participant).
  private static readonly Regex Pattern = new(
    @"^Target\s+creature\s+you\s+control\s+fights\s+target\s+creature\s+(?:you\s+don't\s+control|an\s+opponent\s+controls)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text))
    {
      return false;
    }

    effect = new FightEffect
    {
      Controlled = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
      Opposed = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.Opponent,
        },
      },
    };
    return true;
  }
}
