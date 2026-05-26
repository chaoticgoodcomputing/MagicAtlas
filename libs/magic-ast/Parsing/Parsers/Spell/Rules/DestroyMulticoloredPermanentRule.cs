namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target multicolored permanent." — Null Elemental Blast. Sibling of
/// <see cref="DestroyMonocoloredCreatureRule"/> on <see cref="ObjectFilter.IsMulticolored"/>.
/// </summary>
[SpellRule]
public sealed class DestroyMulticoloredPermanentRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Regex.IsMatch(text, @"^Destroy\s+target\s+multicolored\s+permanent$", RegexOptions.IgnoreCase))
    {
      return false;
    }
    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          IsMulticolored = true,
        },
      },
    };
    return true;
  }
}
