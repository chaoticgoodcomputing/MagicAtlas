namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// "Surveil N" as an activated-ability effect — e.g. "{4}, {T}: Surveil 1."
/// CR 701.25a: "To 'surveil N' means to look at the top N cards of your library,
/// then put any number of them into your graveyard and the rest on top of your
/// library in any order."
/// </summary>
[ActivatedEffectRule(Priority = 999)]
public sealed class SurveilEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');

    var match = Regex.Match(effectText, @"^Surveil\s+(\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return null;
    }

    var count = int.Parse(match.Groups[1].Value);
    return new SurveilEffect { Count = LiteralQuantity.Of(count) };
  }
}
