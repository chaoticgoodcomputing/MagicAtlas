namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;

/// <summary>
/// "Prevent all combat damage that would be dealt this turn."
/// Fog-effect instants (Rule 615.1). Combat damage only; no target (blanket).
/// </summary>
[SpellRule]
public sealed class PreventAllCombatDamageThisTurnRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Prevent\s+all\s+combat\s+damage\s+that\s+would\s+be\s+dealt\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text))
    {
      return false;
    }

    effect = new PreventDamageEffect
    {
      All = true,
      CombatOnly = true,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
