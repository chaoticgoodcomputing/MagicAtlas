namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "tap this creature" / "tap this permanent" / "tap it" — the plain
/// self-tap triggered effect (Rule 701.26, Tap and Untap). Fleshmad Steed:
/// "Whenever another creature dies, tap this creature."
/// </summary>
/// <remarks>
/// Anchored to the WHOLE effect fragment (post trigger-split, post period
/// strip) so this rule does NOT swallow the sibling "tap this creature
/// unless [cost]" conditional family (Carnophage, Sangrophage, Apocalypse
/// Demon, Electrozoa, Heavyweight Demolisher, ...) — a distinct
/// unless-conditional effect shape — nor multi-sentence composites like
/// Drudge Sentinel's "Tap this creature. It gains indestructible until end
/// of turn." (handled, sentence-by-sentence, by the dispatcher's sentence
/// bundle path before reaching the single-rule loop this rule lives in).
/// </remarks>
[TriggeredRule]
public sealed class TapSelfRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^(?<optional>you\s+may\s+)?tap\s+(this\s+\w+|it)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var isOptional = m.Groups["optional"].Success;
    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(
      new TapEffect { Target = ObjectReference.Self() },
      isOptional
    );
    return true;
  }
}
