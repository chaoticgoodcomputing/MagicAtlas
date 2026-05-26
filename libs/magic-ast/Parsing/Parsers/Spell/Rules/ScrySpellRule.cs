namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// "Scry N." — spell-side scry. Rule 701.18. No Player field (implicit you).
/// </summary>
[SpellRule]
public sealed class ScrySpellRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(text, @"^Scry\s+(?<count>\d+)$", RegexOptions.IgnoreCase);
    if (!m.Success)
    {
      return false;
    }
    effect = new ScryEffect
    {
      Count = LiteralQuantity.Of(int.Parse(m.Groups["count"].Value)),
    };
    return true;
  }
}
