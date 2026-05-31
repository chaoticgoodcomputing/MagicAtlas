namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// "All creatures able to block target creature this turn do so." — Lure-type spell.
/// Rule 509.1c forcing requirement applied transiently until end of turn.
/// </summary>
[SpellRule]
public sealed class AllMustBlockTargetRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^All\s+creatures\s+able\s+to\s+block\s+target\s+creature\s+this\s+turn\s+do\s+so$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    effect = new AllMustBlockEffect
    {
      Target = ObjectReference.Target(
        new ObjectFilter { CardTypes = ["creature"] }
      ),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
