namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "untap this artifact" / "untap it" with optional "you may pay {X}. If you do, ..." gate.
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
    var isOptional = Regex.IsMatch(lower, @"you\s+may\s+pay\s+\{[^}]+\}", RegexOptions.IgnoreCase);
    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new UntapEffect { Target = ObjectReference.Self()}, isOptional);
    return true;
  }
}
