namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [type1] or [type2]." / "Destroy target [type1], [type2], or [type3]."
/// Multi-element <see cref="ObjectFilter.CardTypes"/> as the disjunction.
/// </summary>
[SpellRule]
public sealed class DestroyTargetTypeDisjunctionRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Destroy\s+target\s+(?<types>[a-z]+(?:\s*,\s*[a-z]+)*\s+or\s+[a-z]+)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    var cardTypes = SpellRuleHelpers.SplitTypeDisjunction(m.Groups["types"].Value);
    if (cardTypes.Count < 2)
    {
      return false;
    }

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = cardTypes },
      },
    };
    return true;
  }
}
