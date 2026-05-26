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
/// </list>
/// </summary>
[SpellRule]
public sealed class ModifyPTSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+gets\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
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
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = new UntilEndOfTurnDuration(),
    };
    return true;
  }
}
