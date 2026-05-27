namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// "Surveil N." — standalone spell-side surveil. Rule 701.42.
/// Matches the bare keyword invocation after reminder text has been stripped by the
/// dispatcher. Covers cards like Deadly Visit, Consider, Sinister Sabotage, etc.
/// </summary>
[SpellRule]
public sealed class SurveilSpellRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(text, @"^Surveil\s+(?<count>\d+)$", RegexOptions.IgnoreCase);
    if (!m.Success)
    {
      return false;
    }
    effect = new SurveilEffect
    {
      Count = LiteralQuantity.Of(int.Parse(m.Groups["count"].Value)),
    };
    return true;
  }
}
