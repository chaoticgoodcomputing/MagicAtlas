namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// "Add {mana}" — e.g. "Add {G}", "Add {C}{C}{C}", "Add {W}{U}{B}{R}{G}". Also
/// handles "Add one mana of any color" (Crystal Grotto / Chromatic Lantern shape)
/// where the produced mana is a single choice across all five colors.
/// </summary>
[ActivatedEffectRule(Priority = 1000)]
public sealed class AddManaEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    // Normalize whitespace
    effectText = effectText.Trim();

    if (!effectText.StartsWith("Add ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    // Extract the mana portion (everything after "Add" and before optional ".")
    var manaText = effectText[4..].Trim();
    if (manaText.EndsWith('.'))
    {
      manaText = manaText[..^1].Trim();
    }

    // "one mana of any color" — single-pip wildcard production.
    if (
      Regex.IsMatch(
        manaText,
        @"^one\s+mana\s+of\s+any\s+color$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new AddManaEffect { Mana = string.Empty, AnyColor = true };
    }

    // The mana text should be a sequence of mana symbols like "{G}" or "{C}{C}{C}".
    if (string.IsNullOrWhiteSpace(manaText) || !manaText.Contains('{'))
    {
      return null;
    }

    return new AddManaEffect { Mana = manaText, AnyColor = false };
  }
}
