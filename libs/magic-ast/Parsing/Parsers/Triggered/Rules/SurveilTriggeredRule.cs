namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// "surveil N." — Rule 701.42 keyword action on the triggered side.
/// </summary>
[TriggeredRule]
public sealed class SurveilTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var match = Regex.Match(trimmed, @"^surveil\s+(\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return false;
    }
    var count = int.Parse(match.Groups[1].Value);
    effect = new SurveilEffect { Count = LiteralQuantity.Of(count) };
    return true;
  }
}
