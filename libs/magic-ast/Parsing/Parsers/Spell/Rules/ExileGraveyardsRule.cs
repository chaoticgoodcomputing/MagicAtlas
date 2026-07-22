namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
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
        // "any number of target players'" — an unbounded multi-target choice (CR 115),
        // the cards being those owned by those target players (CR 108.3).
        Quantity = new AnyAmountQuantity(),
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Zone = Zone.Graveyard,
          Owner = ControllerFilter.Target,
        },
      },
    };
    return true;
  }
}
