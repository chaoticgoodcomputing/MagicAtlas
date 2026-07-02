namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// Handles "Target creature can't be blocked this turn." — emits a
/// <see cref="CantBeBlockedEffect"/> targeting a single creature with an
/// <c>untilEndOfTurn</c> duration. Rule 509.1b (evasion ability; the defending
/// player may not declare any creature as a blocker for the named creature
/// during the declare-blockers step of the current turn). Representative cards:
/// Artful Dodge, Slip Through Space.
/// </summary>
[SpellRule]
public sealed class TargetCantBeBlockedThisTurnRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+creature\s+can't\s+be\s+blocked\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    if (!Pattern.IsMatch(text))
      return false;

    effect = new CantBeBlockedEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
        },
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
