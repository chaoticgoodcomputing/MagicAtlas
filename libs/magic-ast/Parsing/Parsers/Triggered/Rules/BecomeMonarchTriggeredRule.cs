namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "you become the monarch." — Rule 725. The controller of the source permanent
/// becomes the monarch game-state designation. MAST records the descriptive
/// instruction; the draw-at-end-step and combat-damage-transfer rules (725.2–725.6)
/// are engine territory.
/// </summary>
[TriggeredRule]
public sealed class BecomeMonarchTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!Regex.IsMatch(trimmed, @"^you\s+become\s+the\s+monarch$", RegexOptions.IgnoreCase))
    {
      return false;
    }
    effect = new BecomeMonarchEffect
    {
      Player = ObjectReference.You(),
    };
    return true;
  }
}
