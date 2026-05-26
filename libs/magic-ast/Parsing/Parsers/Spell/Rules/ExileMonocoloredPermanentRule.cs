namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target monocolored permanent." — Vanishing Verse.
/// </summary>
[SpellRule]
public sealed class ExileMonocoloredPermanentRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Regex.IsMatch(text, @"^Exile\s+target\s+monocolored\s+permanent$", RegexOptions.IgnoreCase))
    {
      return false;
    }
    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          IsMonocolored = true,
        },
      },
    };
    return true;
  }
}
