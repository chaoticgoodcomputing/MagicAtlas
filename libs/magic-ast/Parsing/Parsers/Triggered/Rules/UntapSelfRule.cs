namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "untap this artifact" / "untap it" with optional gate forms:
///   - "you may pay {X}. If you do, untap this artifact." — Mana Vault style (CR 602)
///   - "you may untap this [type]" — Grinding Station style (CR 117.7 "you may")
/// </summary>
[TriggeredRule]
public sealed class UntapSelfRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();
    if (!Regex.IsMatch(lower, @"untap\s+(this|it)\b"))
    {
      return false;
    }
    // Two optional-wrapper forms:
    // 1. "you may pay {X}. If you do, untap ..." — the payment gate (Mana Vault, CR 602)
    // 2. "you may untap this ..." — the plain "you may" prefix on the untap verb (CR 117.7)
    var isOptional =
      Regex.IsMatch(lower, @"you\s+may\s+pay\s+\{[^}]+\}", RegexOptions.IgnoreCase)
      || Regex.IsMatch(lower, @"you\s+may\s+untap\b", RegexOptions.IgnoreCase);
    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new UntapEffect { Target = ObjectReference.Self()}, isOptional);
    return true;
  }
}
