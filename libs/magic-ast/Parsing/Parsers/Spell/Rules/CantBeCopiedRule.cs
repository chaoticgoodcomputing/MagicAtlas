namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "This spell can't be copied." — encoded as a spell-level effect scoped to the
/// spell itself (CR 707, copying spells). The self-spell sibling of
/// <see cref="CantBeCounteredRule"/>; anchored to the exact self-spell surface so it
/// cannot swallow a more-specific "can't be copied" clause on another subject.
/// </summary>
[SpellRule]
public sealed class CantBeCopiedRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Regex.IsMatch(text, @"^This\s+spell\s+can'?t\s+be\s+copied$", RegexOptions.IgnoreCase))
    {
      return false;
    }
    effect = new CantBeCopiedEffect();
    return true;
  }
}
