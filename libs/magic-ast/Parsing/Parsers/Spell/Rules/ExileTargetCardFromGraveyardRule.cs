namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile target card from a graveyard." / "Exile up to one target card from a graveyard." —
/// single-card graveyard-exile spell effect (Rule 701.7). Handles both the bare
/// "target card" form (Purify the Grave) and the "up to one target card" form (Heritage Reclamation).
/// Priority 80: must override <see cref="ExileGraveyardsRule"/> which targets
/// graveyards themselves (the prior agent's noted ordering pair).
/// </summary>
[SpellRule(Priority = 80)]
public sealed class ExileTargetCardFromGraveyardRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // "Exile target card from a graveyard." — bare single-target form.
    if (Regex.IsMatch(text, @"^Exile\s+target\s+card\s+from\s+a\s+graveyard$", RegexOptions.IgnoreCase))
    {
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
        },
      };
      return true;
    }

    // "Exile up to one target card from a graveyard." — bounded-count form.
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
