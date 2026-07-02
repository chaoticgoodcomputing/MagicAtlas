namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;

/// <summary>
/// "it connives." — Rule 701.50a keyword action on the triggered side.
/// Matches the canonical ETB-connive pattern where the entering creature
/// (referred to as "it") performs the Connive keyword action.
/// </summary>
[TriggeredRule]
public sealed class ConniveTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var match = Regex.Match(trimmed, @"^it\s+connives$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return false;
    }
    effect = new ConniveEffect { Target = ObjectReference.It() };
    return true;
  }
}
