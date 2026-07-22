namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target [X] from [zone] to the battlefield." — e.g. "Return target
/// creature or Vehicle card from your graveyard to the battlefield." The full
/// type-phrase is captured as a Characteristics entry and the source zone on Zone.
/// </summary>
[ActivatedEffectRule(Priority = 988)]
public sealed class ReturnToBattlefieldEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Regex.Match(
      trimmed,
      @"^return\s+target\s+(?<what>.+?)\s+from\s+(?:your|the|a|an)\s+(?<zone>graveyard|hand|library|exile)\s+to\s+the\s+battlefield$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var what = m.Groups["what"].Value.Trim();
    var zoneRaw = m.Groups["zone"].Value.ToLowerInvariant();

    var zone = zoneRaw switch
    {
      "graveyard" => Zone.Graveyard,
      "hand" => Zone.Hand,
      "library" => Zone.Library,
      "exile" => Zone.Exile,
      _ => Zone.Graveyard,
    };

    // "creature or Vehicle card" — a cross-axis type disjunction (card type ∨ subtype);
    // structure it onto ObjectFilter.AnyOf. Otherwise the whole type-phrase is captured as a
    // single Characteristic (FromLabel routes recognised keywords to their structured axis and
    // the rest to the typed residual).
    var anyOf = what.Contains(" or ", StringComparison.OrdinalIgnoreCase)
      ? TypeDisjunctionParser.TryParse(what)
      : null;

    var filter = anyOf is not null
      ? new ObjectFilter { Zone = zone, AnyOf = anyOf }
      : new ObjectFilter { Zone = zone, Characteristics = [Characteristic.FromLabel(what)] };

    return new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
    };
  }
}
