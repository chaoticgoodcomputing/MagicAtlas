namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "This spell can't be countered." — encoded as a spell-level effect.
/// </summary>
[SpellRule]
public sealed class CantBeCounteredRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Regex.IsMatch(text, @"^This\s+spell\s+can'?t\s+be\s+countered$", RegexOptions.IgnoreCase))
    {
      return false;
    }
    effect = new CantBeCounteredEffect();
    return true;
  }
}
