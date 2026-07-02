namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;

/// <summary>
/// "proliferate." — Rule 701.27 keyword action on the triggered side.
/// Matches the bare proliferate instruction that appears after the trigger comma
/// (reminder text has already been stripped by the time this rule is called).
/// </summary>
[TriggeredRule]
public sealed class ProliferateTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!Regex.IsMatch(trimmed, @"^proliferate$", RegexOptions.IgnoreCase))
    {
      return false;
    }
    effect = new ProliferateEffect();
    return true;
  }
}
