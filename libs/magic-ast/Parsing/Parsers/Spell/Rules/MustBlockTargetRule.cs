namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// "Target creature blocks this turn if able." — Culling Mark. Rule 509.1c.
/// </summary>
[SpellRule]
public sealed class MustBlockTargetRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Target\s+(?<type>creature)\s+blocks\s+this\s+turn\s+if\s+able$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    effect = new MustBlockEffect
    {
      Target = ObjectReference.Target(
        new ObjectFilter { CardTypes = [m.Groups["type"].Value.ToLowerInvariant()] }
      ),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
