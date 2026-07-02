namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Tap X target creatures." — variable-count tap-targets spell effect, where the
/// count is the spell's mana-cost X (CR 107.3 / 107.3a: the controller chooses X as
/// the spell is cast; here X also determines how many creatures are tapped).
/// The variable lives on <see cref="TapEffect.Count"/> as a <see cref="VariableQuantity"/>
/// (NOT on <see cref="ObjectReference.Quantity"/>, which is reserved for the
/// "up to N target" phrasing handled by <see cref="TapUpToNTargetsRule"/>).
/// CR 701.26 / 701.26a ("Tap and Untap").
/// Priority 60: above the generic <see cref="SpellTapTargetRule"/> (default 50),
/// whose count path only understands literal/word counts via
/// <see cref="SpellRuleHelpers.TryParseSmallWord"/> and returns false on "X".
/// </summary>
[SpellRule(Priority = 60)]
public sealed class TapXTargetsRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Tap\s+(?<var>[XYZ])\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var typesPhrase = m.Groups["types"].Value;
    var types = Regex
      .Split(typesPhrase, @"\s*,\s*|\s+or\s+")
      .Select(t => t.Trim().ToLowerInvariant())
      .Select(t => t.EndsWith("s") && t.Length > 1 ? t[..^1] : t)
      .Where(t => t.Length > 0)
      .ToList();

    if (types.Count == 0)
    {
      return false;
    }

    effect = new TapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = types },
      },
      Count = new VariableQuantity { Name = m.Groups["var"].Value.ToUpperInvariant() },
    };
    return true;
  }
}
