namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;

/// <summary>
/// "Adapt N" — e.g. "Adapt 2" (CR 701.46a: "\"Adapt N\" means \"If this permanent
/// has no +1/+1 counters on it, put N +1/+1 counters on it.\"")
/// The trailing parenthetical reminder is stripped by the caller before this rule
/// is invoked.
/// </summary>
[ActivatedEffectRule(Priority = 999)]
public sealed class AdaptEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');

    var match = Regex.Match(effectText, @"^Adapt\s+(\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return null;
    }

    var count = int.Parse(match.Groups[1].Value);
    return new AdaptEffect { Count = LiteralQuantity.Of(count) };
  }
}
