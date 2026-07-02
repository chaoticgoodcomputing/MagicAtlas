namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Recognises the P/T-switch shape (CR 613.4d — layer 7d): "take the value of power
/// and apply it to the creature's toughness, and take the value of toughness and
/// apply it to the creature's power."
///   "Switch target creature's power and toughness until end of turn."
///
/// Examples:
/// <list type="bullet">
///   <item>"Switch target creature's power and toughness until end of turn."  (Twisted Image)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class SwitchTargetCreaturePTSpellRule : ISpellRule
{
  private static readonly Regex _switchTargetCreaturePattern = new(
    @"^Switch\s+target\s+creature's\s+power\s+and\s+toughness\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var m = _switchTargetCreaturePattern.Match(trimmed);
    if (m.Success)
    {
      effect = new SwitchPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        Duration = UntilTimeDuration.EndOfTurn,
      };
      return true;
    }

    return false;
  }
}
