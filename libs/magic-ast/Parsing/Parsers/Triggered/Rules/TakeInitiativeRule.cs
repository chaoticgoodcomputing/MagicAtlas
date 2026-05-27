namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "you take the initiative." — Rule 726. The player takes the initiative
/// game-state designation. Parameterless; mirrors AscendEffect doctrine.
/// </summary>
[TriggeredRule]
public sealed class TakeInitiativeRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!Regex.IsMatch(trimmed, @"^you\s+take\s+the\s+initiative$", RegexOptions.IgnoreCase))
    {
      return false;
    }
    effect = new TakeInitiativeEffect();
    return true;
  }
}
