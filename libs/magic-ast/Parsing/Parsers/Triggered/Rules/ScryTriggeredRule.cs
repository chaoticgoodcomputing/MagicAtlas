namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// "scry N." — Rule 701.18 keyword action on the triggered side.
/// </summary>
[TriggeredRule]
public sealed class ScryTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var match = Regex.Match(trimmed, @"^scry\s+(\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return false;
    }
    var count = int.Parse(match.Groups[1].Value);
    effect = new ScryEffect { Count = LiteralQuantity.Of(count) };
    return true;
  }
}
