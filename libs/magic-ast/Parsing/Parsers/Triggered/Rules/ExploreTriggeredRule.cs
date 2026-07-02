namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;

/// <summary>
/// "it explores." — Rule 701.44a keyword action on the triggered side.
/// Matches the canonical ETB-explore pattern where the entering creature
/// (referred to as "it") performs the Explore keyword action.
/// </summary>
[TriggeredRule]
public sealed class ExploreTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var match = Regex.Match(trimmed, @"^it\s+explores$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return false;
    }
    effect = new ExploreEffect { Target = ObjectReference.It() };
    return true;
  }
}
