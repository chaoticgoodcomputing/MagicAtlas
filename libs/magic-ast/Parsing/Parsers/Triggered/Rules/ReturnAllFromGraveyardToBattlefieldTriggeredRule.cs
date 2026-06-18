namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return all [type] cards from your graveyard to the battlefield [tapped]."
/// Mass-recursion pattern as a triggered-ability effect — e.g. the second clause
/// of Lumra, Bellow of the Woods's ETB:
/// "Then return all land cards from your graveyard to the battlefield tapped."
///
/// Parallel to <see cref="Activated.Rules.ReturnAllFromGraveyardToBattlefieldRule"/>
/// (the same semantic pattern on the activated-ability side); split into a
/// separate file because <see cref="ITriggeredRule"/> and
/// <see cref="Activated.IActivatedEffectRule"/> are distinct interfaces.
///
/// Handles an optional leading "Then " prefix (produced by the sentence-bundle
/// splitter in <see cref="TriggeredAbilityParser"/> when the return clause follows
/// another sentence, e.g. "mill four cards. Then return all land cards …").
///
/// CR 701 (zone change); CR 400.1 (zones); the optional "tapped" entering status
/// is CR 110.5b. Fully anchored (^…$) to prevent substring collision with
/// single-target return rules.
/// </summary>
[TriggeredRule(Priority = 989)]
public sealed class ReturnAllFromGraveyardToBattlefieldTriggeredRule : ITriggeredRule
{
  /// <summary>
  /// Matches:
  ///   optional "Then " prefix (from sentence-bundle splitter)
  ///   "return all [type] cards from your graveyard to the battlefield"
  ///   optional trailing " tapped"
  ///
  /// Anchored (^…$) — cannot match inside a longer clause.
  /// </summary>
  private static readonly Regex Pattern = new(
    @"^(?:then\s+)?return\s+all\s+(?<type>\w+(?:\s+\w+)?)\s+cards?\s+from\s+(?:your|the)\s+graveyard\s+to\s+the\s+battlefield(?:\s+(?<tapped>tapped))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// The card types (CR 300.1 / 205.2a) that can be returned to the battlefield.
  /// Only a single, recognised card-type word maps cleanly; a subtype-qualified
  /// phrase ("Zombie creature") or an unrecognised word would corrupt CardTypes,
  /// so the rule bails rather than mislabelling (mirroring the activated counterpart).
  /// </summary>
  private static readonly HashSet<string> ReturnableCardTypes = new(
    StringComparer.OrdinalIgnoreCase
  )
  {
    "land",
    "creature",
    "artifact",
    "enchantment",
    "planeswalker",
    "permanent",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var typeRaw = m.Groups["type"].Value.Trim();
    if (!ReturnableCardTypes.Contains(typeRaw))
    {
      // Unrecognised type phrase — fall through rather than emit a malformed filter.
      return false;
    }

    var tapped = m.Groups["tapped"].Success;

    effect = new ReturnToBattlefieldEffect
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
    return true;
  }
}
