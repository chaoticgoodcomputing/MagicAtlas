namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// "add an additional {X}" — triggered mana-doubling effect. This is the effect clause
/// of a mana-doubling trigger (e.g. Forsaken Monument: "Whenever you tap a permanent for
/// {C}, add an additional {C}"). The word "additional" distinguishes this from the baseline
/// <see cref="AddManaRule"/> which handles "add {X}" directly.
///
/// Rule 106: "To add mana, a player puts that mana into their mana pool." The "additional"
/// qualifier is descriptive context for the trigger (you get one more mana of that type on
/// top of what the tapping already produced); MAST models what the oracle text says — the
/// effect is an addMana node whose <c>Mana</c> field holds the mana symbol string.
/// </summary>
[TriggeredRule]
public sealed class AddAdditionalManaRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^add\s+an?\s+additional\s+(?<mana>\{[A-Z0-9/]+\})$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var t = text.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(t);
    if (!m.Success)
    {
      return false;
    }

    var mana = m.Groups["mana"].Value.ToUpperInvariant();
    effect = new AddManaEffect { Mana = mana };
    return true;
  }
}
