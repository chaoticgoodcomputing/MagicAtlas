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

    // Strip optional "you may" prefix before the add verb.
    var isOptional = false;
    if (t.StartsWith("you may ", System.StringComparison.OrdinalIgnoreCase))
    {
      isOptional = true;
      t = t[8..].Trim();
    }

    if (!t.StartsWith("add ", System.StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    var manaText = t[4..].Trim();
    if (Regex.IsMatch(manaText, @"^one\s+mana\s+of\s+any\s+color$", RegexOptions.IgnoreCase))
    {
      effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new AddManaEffect { Mana = string.Empty, AnyColor = true}, isOptional);
      return true;
    }
    // "one mana of any type that permanent produced" — Kinnan, Bonder Prodigy: the added mana
    // mirrors the type produced by the triggering tap event (may be W, U, B, R, G, or C).
    if (Regex.IsMatch(manaText, @"^one\s+mana\s+of\s+any\s+type\s+that\s+permanent\s+produced$", RegexOptions.IgnoreCase))
    {
      effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new AddManaEffect { Mana = string.Empty, AnyType = true }, isOptional);
      return true;
    }
    if (string.IsNullOrWhiteSpace(manaText) || !manaText.Contains('{'))
    {
      return false;
    }
    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new AddManaEffect { Mana = manaText, AnyColor = false}, isOptional);
    return true;
  }
}
