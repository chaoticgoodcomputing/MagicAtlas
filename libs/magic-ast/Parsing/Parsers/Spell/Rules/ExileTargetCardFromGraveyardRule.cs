namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile up to one target card from a graveyard." — Heritage Reclamation modal.
/// Priority 80: must override <see cref="ExileGraveyardsRule"/> which targets
/// graveyards themselves (the prior agent's noted ordering pair).
/// </summary>
[SpellRule(Priority = 80)]
public sealed class ExileTargetCardFromGraveyardRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Exile\s+up\s+to\s+(?<n>one)\s+target\s+card\s+from\s+a\s+graveyard$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    var maximum = SpellRuleHelpers.ParseSmallWord(m.Groups["n"].Value);
    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Zone = Zone.Graveyard,
        },
        Quantity = new UpToQuantity { Maximum = maximum, Minimum = 0 },
      },
    };
    return true;
  }
}
