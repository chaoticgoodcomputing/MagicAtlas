namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// "Scry N" — e.g. "Scry 2", "Scry 1" (Rule 701.18).
/// </summary>
[ActivatedEffectRule(Priority = 999)]
public sealed class ScryEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');

    var match = Regex.Match(effectText, @"^Scry\s+(\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return null;
    }

    var count = int.Parse(match.Groups[1].Value);
    return new ScryEffect { Count = LiteralQuantity.Of(count) };
  }
}
