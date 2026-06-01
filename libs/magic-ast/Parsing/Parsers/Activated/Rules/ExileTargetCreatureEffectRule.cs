namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target creature." — moves a targeted creature from whatever zone it
/// occupies to the exile zone.
///
/// Also handles the Aura/Equipment form: "Exile enchanted creature." — exiles the
/// permanent this card is attached to (CR 303.4m). Not a targeted ability; no
/// "target" keyword in oracle text.
///
/// CR 701.13a: "To exile an object, move it to the exile zone from wherever it is."
/// CR 303.4m: "An ability of a permanent that refers to the 'enchanted [object or player]'
/// refers to whatever object or player that permanent is attached to…"
/// </summary>
[ActivatedEffectRule(Priority = 983)]
public sealed class ExileTargetCreatureEffectRule : IActivatedEffectRule
{
  private static readonly Regex _targetPattern = new(
    @"^Exile\s+target\s+creature\s*\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _enchantedPattern = new(
    @"^Exile\s+(?:enchanted|equipped)\s+\w+\s*\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();

    if (_targetPattern.IsMatch(text))
    {
      return new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
          },
        },
      };
    }

    // Aura/Equipment form: "Exile enchanted creature." / "Exile equipped creature."
    // The attached object is referenced as EnchantedOrEquipped; not a targeted ability.
    if (_enchantedPattern.IsMatch(text))
    {
      return new ExileEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      };
    }

    return null;
  }
}
