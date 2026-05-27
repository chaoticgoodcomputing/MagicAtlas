namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "tap enchanted [type]" — Aura enters-the-battlefield tap effect.
/// Fires when an Aura's triggered ability instructs it to tap the enchanted
/// permanent. The target is always the permanent the Aura is attached to,
/// modelled as <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>.
/// Rule 701.26 (Tap and Untap); Rule 303.4 (Aura attachment).
/// </summary>
[TriggeredRule]
public sealed class TapEnchantedTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^tap\s+enchanted\s+\w+$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new TapEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      IsOptional = false,
    };
    return true;
  }
}
