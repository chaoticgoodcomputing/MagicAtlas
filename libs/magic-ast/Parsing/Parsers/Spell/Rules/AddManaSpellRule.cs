namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// "Add {mana}." — bare add-mana spell instruction (e.g., Infernal Plunge's main effect).
/// Covers the same oracle pattern that ActivatedAbilityParser handles on the right-hand
/// side of a mana ability cost, but here it appears as the whole spell body after the
/// additional-cost prefix has been stripped by ClauseSplitter.
/// </summary>
[SpellRule]
public sealed class AddManaSpellRule : ISpellRule
{
  // "Add" followed by one or more mana symbols: "{R}{R}{R}", "{G}", "{W}{U}", etc.
  private static readonly Regex Pattern = new(
    @"^Add\s+(?<mana>(\{[^}]+\})+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // "Add one mana of any color."
  private static readonly Regex AnyColorPattern = new(
    @"^Add\s+one\s+mana\s+of\s+any\s+color$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    if (AnyColorPattern.IsMatch(text))
    {
      effect = new AddManaEffect { Mana = string.Empty, AnyColor = true };
      return true;
    }

    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    effect = new AddManaEffect { Mana = m.Groups["mana"].Value, AnyColor = false };
    return true;
  }
}
