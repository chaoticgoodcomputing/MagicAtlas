namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// "add {...}" / "add one mana of any color" — Rule 106. Note: triggered mana
/// production is NOT a Rule 605 mana ability, so no IsManaAbility flag is set.
/// </summary>
[TriggeredRule]
public sealed class AddManaRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var t = text.Trim().TrimEnd('.').Trim();
    if (!t.StartsWith("add ", System.StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    var manaText = t[4..].Trim();
    if (Regex.IsMatch(manaText, @"^one\s+mana\s+of\s+any\s+color$", RegexOptions.IgnoreCase))
    {
      effect = new AddManaEffect { Mana = string.Empty, AnyColor = true };
      return true;
    }
    if (string.IsNullOrWhiteSpace(manaText) || !manaText.Contains('{'))
    {
      return false;
    }
    effect = new AddManaEffect { Mana = manaText, AnyColor = false };
    return true;
  }
}
