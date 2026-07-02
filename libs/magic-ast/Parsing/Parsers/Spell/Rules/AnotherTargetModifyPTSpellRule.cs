namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the bare "another target" P/T-modification shape as a standalone
/// sentence (distinct from <see cref="ModifyPTSpellRule"/>'s "Target creature"
/// shape):
///   "Another target creature gets +N/+M until end of turn."
///   "Another target creature gets -N/-M until end of turn."
///
/// Per CR 601.2c, a spell that uses the word "target" multiple times chooses an
/// independent object for each instance; CR 115.4 names the "another target"
/// phrasing. The "another" qualifier (CR 109.5's "another" convention isn't in the
/// bundled rules-structure.json — see codebase convention at GLOSSARY.md line 5655
/// and the accepted M21/RookieMistake.json gold) is modelled as
/// <see cref="ObjectFilter.ExcludeSelf"/> = true, ensuring the second target can't
/// be the same object chosen for the first "target creature" in the sentence
/// preceding this one in the sentence-bundle dispatch.
///
/// Examples:
/// <list type="bullet">
///   <item>"Another target creature gets -1/-1 until end of turn."  (Leeching Bite — second sentence)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class AnotherTargetModifyPTSpellRule : ISpellRule
{
  private static readonly Regex _anotherTargetCreaturePattern = new(
    @"^Another\s+target\s+creature\s+gets\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var m = _anotherTargetCreaturePattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);
    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"], ExcludeSelf = true },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
