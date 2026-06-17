namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return all [type] cards from your graveyard to the battlefield [tapped]."
/// Mass-recursion pattern — e.g. Aftermath Analyst's activated ability.
/// CR 701 (zone change); CR 400.1 (zones); the optional "tapped" entering status is CR 110.5b.
/// Anchored on "Return all" prefix so it cannot match sibling single-target rules.
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class ReturnAllFromGraveyardToBattlefieldRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^return\s+all\s+(?<type>\w+(?:\s+\w+)?)\s+cards?\s+from\s+(?:your|the)\s+graveyard\s+to\s+the\s+battlefield(?:\s+(?<tapped>tapped))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // The card types (CR 300.1 / 205.2a) that can be returned to the battlefield. Only a single,
  // recognized card-type word maps cleanly; a subtype-qualified phrase ("Zombie creature") or an
  // unrecognized word would corrupt CardTypes, so we bail rather than mislabel (the prior default
  // branch dumped the raw captured phrase straight into CardTypes — a latent sibling-mislabel).
  private static readonly HashSet<string> ReturnableCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "land", "creature", "artifact", "enchantment", "planeswalker", "permanent",
  };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var typeRaw = m.Groups["type"].Value.Trim();
    if (!ReturnableCardTypes.Contains(typeRaw))
    {
      // Not a recognized single card type (subtype-qualified or unrelated) — let another rule
      // handle it rather than writing a malformed CardTypes entry.
      return null;
    }

    var tapped = m.Groups["tapped"].Success;

    return new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = [typeRaw.ToLowerInvariant()],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
      Tapped = tapped,
    };
  }
}
