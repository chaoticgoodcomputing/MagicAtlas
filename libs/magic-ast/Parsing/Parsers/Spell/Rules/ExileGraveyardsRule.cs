namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile any number of target players' graveyards." — Death of Gwen Stacy ch. 3.
/// </summary>
[SpellRule]
public sealed class ExileGraveyardsRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Regex.IsMatch(
        text,
        @"^Exile\s+(?:any\s+number\s+of\s+)?target\s+players'?\s+graveyards?$",
        RegexOptions.IgnoreCase))
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
          CardTypes = ["card"],
          Zone = Zone.Graveyard,
          Characteristics = ["in any number of target players' graveyards"],
        },
      },
    };
    return true;
  }
}
