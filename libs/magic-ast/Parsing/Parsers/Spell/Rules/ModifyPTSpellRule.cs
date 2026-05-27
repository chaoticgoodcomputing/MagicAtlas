namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the bare P/T-modification shape (no keyword conjunction):
///   "Target creature gets +N/+M until end of turn."
///   "Target creature gets -N/-M until end of turn."
///   "Target creature gets +X/+0 until end of turn."  — variable X form
///   "It gets +N/+M until end of turn."   — pronoun back-reference form
///
/// This is the simple single-effect version. The composite "gets +N/+M and gains
/// &lt;keyword&gt; until end of turn" case is handled separately by
/// <see cref="ModifyPTAndGainKeywordSpellRule"/> and is tried first at higher priority.
///
/// Examples:
/// <list type="bullet">
///   <item>"Target creature gets +3/+3 until end of turn."  (Giant Growth)</item>
///   <item>"Target creature gets +4/+4 until end of turn."  (Titanic Growth)</item>
///   <item>"Target creature gets -2/-2 until end of turn."  (Disfigure)</item>
///   <item>"Target creature gets -3/-3 until end of turn."  (Last Gasp)</item>
///   <item>"Target creature gets +X/+0 until end of turn."  (Howl from Beyond)</item>
///   <item>"It gets +2/+4 until end of turn."              (Inspirit — second sentence)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class ModifyPTSpellRule : ISpellRule
{
  private static readonly Regex _targetCreaturePattern = new(
    @"^Target\s+creature\s+gets\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "Target creature gets +X/+0 until end of turn" — variable X for power, literal for toughness.
  // Captures: <pvar> = variable name (X/Y/Z), <t> = literal toughness modifier.
  private static readonly Regex _targetCreatureVariablePattern = new(
    @"^Target\s+creature\s+gets\s+(?<psign>[+\-])(?<pvar>[XYZ])/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "It gets +N/+M until end of turn" — pronoun back-reference after an untap or similar effect.
  private static readonly Regex _itGetsPattern = new(
    @"^It\s+gets\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var m = _targetCreaturePattern.Match(trimmed);
    if (m.Success)
    {
      var power = int.Parse(m.Groups["p"].Value);
      var toughness = int.Parse(m.Groups["t"].Value);
      effect = new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = LiteralQuantity.Of(power),
        ToughnessModifier = LiteralQuantity.Of(toughness),
        Duration = new UntilEndOfTurnDuration(),
      };
      return true;
    }

    var mv = _targetCreatureVariablePattern.Match(trimmed);
    if (mv.Success)
    {
      var varName = mv.Groups["pvar"].Value.ToUpperInvariant();
      var toughness = int.Parse(mv.Groups["t"].Value);
      effect = new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = new VariableQuantity { Name = varName },
        ToughnessModifier = LiteralQuantity.Of(toughness),
        Duration = new UntilEndOfTurnDuration(),
      };
      return true;
    }

    var it = _itGetsPattern.Match(trimmed);
    if (it.Success)
    {
      var power = int.Parse(it.Groups["p"].Value);
      var toughness = int.Parse(it.Groups["t"].Value);
      effect = new ModifyPTEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.It },
        PowerModifier = LiteralQuantity.Of(power),
        ToughnessModifier = LiteralQuantity.Of(toughness),
        Duration = new UntilEndOfTurnDuration(),
      };
      return true;
    }

    return false;
  }
}
