namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target monocolored creature." — Ultimate Price. The "monocolored"
/// qualifier surfaces on <see cref="ObjectFilter.IsMonocolored"/>.
/// </summary>
[SpellRule]
public sealed class DestroyMonocoloredCreatureRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Regex.IsMatch(text, @"^Destroy\s+target\s+monocolored\s+creature$", RegexOptions.IgnoreCase))
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
          CardTypes = ["creature"],
          IsMonocolored = true,
        },
      },
    };
    return true;
  }
}
