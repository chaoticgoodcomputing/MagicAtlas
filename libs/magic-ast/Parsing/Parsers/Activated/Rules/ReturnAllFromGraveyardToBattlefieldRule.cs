namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return all [type] cards from your graveyard to the battlefield [tapped]."
/// Mass land-recursion pattern — e.g. Aftermath Analyst's activated ability.
/// CR 701 (zone change); CR 400.1 (zones); "tapped" modifier (CR 110.6).
/// Anchored on "Return all" prefix so it cannot match sibling single-target rules.
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class ReturnAllFromGraveyardToBattlefieldRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^return\s+all\s+(?<type>\w+(?:\s+\w+)?)\s+cards?\s+from\s+(?:your|the)\s+graveyard\s+to\s+the\s+battlefield(?:\s+(?<tapped>tapped))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var typeRaw = m.Groups["type"].Value.Trim().ToLowerInvariant();
    var tapped = m.Groups["tapped"].Success;

    // Map recognized type words to card type. Currently only "land" is needed but
    // the rule is written generically so it degrades gracefully for future shapes.
    // "land" is a card type (CR 205.3a); keep as-is in CardTypes.
    var filter = typeRaw switch
    {
      "land" => new ObjectFilter
      {
        CardTypes = ["land"],
        Zone = Zone.Graveyard,
        Controller = ControllerFilter.You,
      },
      _ => new ObjectFilter
      {
        CardTypes = [typeRaw],
        Zone = Zone.Graveyard,
        Controller = ControllerFilter.You,
      },
    };

    return new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = filter,
      },
      Tapped = tapped,
    };
  }
}
