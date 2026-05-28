namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.References;

/// <summary>
/// "Target player becomes the monarch." — Rule 716 monarch designation granted to
/// a chosen player.
/// </summary>
[ActivatedEffectRule(Priority = 991)]
public sealed class BecomeMonarchEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (
      !Regex.IsMatch(
        trimmed,
        @"^Target\s+player\s+becomes\s+the\s+monarch$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return null;
    }
    return new MagicAST.AST.Effects.Timing.BecomeMonarchEffect
    {
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player"] },
      },
    };
  }
}
