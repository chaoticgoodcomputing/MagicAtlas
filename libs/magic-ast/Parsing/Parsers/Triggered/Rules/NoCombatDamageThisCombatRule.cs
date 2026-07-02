namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// "This creature assigns no combat damage this combat." — the source creature
/// is excluded from assigning combat damage for the remainder of the current combat.
///
/// <para>
/// Rule 510.1: "Each attacking creature and each blocking creature assigns combat
/// damage." This effect records the explicit oracle override preventing the named
/// creature from assigning any combat damage in the current combat phase. Descriptive
/// only; the zero-damage assignment is engine territory.
/// </para>
///
/// <para>
/// Canonical use: Master of Cruelties — the triggered ability that sets an attacked
/// player's life total to 1 also prevents the creature from dealing combat damage, so
/// the combined effect is: reduce to 1 life without also dealing the 1/4 base damage.
/// Oracle text: "This creature assigns no combat damage this combat."
/// </para>
/// </summary>
[TriggeredRule]
public sealed class NoCombatDamageThisCombatRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^This\s+creature\s+assigns\s+no\s+combat\s+damage\s+this\s+combat$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new NoCombatDamageEffect
    {
      Source = ObjectReference.Self(),
    };
    return true;
  }
}
