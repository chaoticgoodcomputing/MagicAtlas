namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the X-scaled P/T-modification shape where BOTH power and toughness
/// are the same announced variable:
///   "Target creature gets +X/+X until end of turn."
///
/// Per CR 107.3a, the controller announces the value of X when casting the spell.
/// Per CR 107.3i, "Normally, all instances of X on an object have the same value at
/// any given time" — so the power and toughness modifiers here share the same
/// <see cref="VariableQuantity"/> name. Per CR 613.4c (Layer 7c), this is a P/T
/// -modifying effect (not a set-P/T effect), so it is modeled as
/// <see cref="ModifyPTEffect"/>.
///
/// This pattern requires a variable (X/Y/Z) in BOTH the power and toughness slots
/// and is mutually exclusive with <see cref="ModifyPTSpellRule"/>'s patterns, which
/// all require a literal digit in at least the toughness slot.
///
/// Examples:
/// <list type="bullet">
///   <item>"Target creature gets +X/+X until end of turn."  (Untamed Might)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class ModifyPTBothVariableSpellRule : ISpellRule
{
  private static readonly Regex _targetCreatureBothVariablePattern = new(
    @"^Target\s+creature\s+gets\s+(?<psign>[+\-])(?<pvar>[XYZ])/(?<tsign>[+\-])(?<tvar>[XYZ])\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var m = _targetCreatureBothVariablePattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var powerVarName = m.Groups["pvar"].Value.ToUpperInvariant();
    var toughnessVarName = m.Groups["tvar"].Value.ToUpperInvariant();

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      PowerModifier = new VariableQuantity { Name = powerVarName },
      ToughnessModifier = new VariableQuantity { Name = toughnessVarName },
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
