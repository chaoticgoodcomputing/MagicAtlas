namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// "Target creature must be blocked this turn if able." — Irresistible Prey. Rule 509.1c.
/// </summary>
[SpellRule]
public sealed class MustBeBlockedTargetRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Target\s+(?<type>creature)\s+must\s+be\s+blocked\s+this\s+turn\s+if\s+able$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    effect = new MustBeBlockedEffect
    {
      Target = ObjectReference.Target(
        new ObjectFilter { CardTypes = [m.Groups["type"].Value.ToLowerInvariant()] }
      ),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
