namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "draw a card, then you may put a [filter] card from your hand onto the battlefield"
///
/// <para>
/// Handles the Spelunking ETB pattern: a two-step action within a single sentence
/// where the controller draws a card and then optionally puts a qualifying card from
/// their hand onto the battlefield. The two sub-effects are emitted as a
/// <see cref="CompositeEffect"/> so the sentence-bundle dispatcher's one-effect-per-sentence
/// invariant is preserved while the full semantics of the sentence are captured.
/// </para>
///
/// <para>
/// Rule citations:
/// </para>
/// <list type="bullet">
///   <item>CR 121.1: "A player draws a card by putting the top card of that player's library
///   into that player's hand."</item>
///   <item>CR 603.1: "Triggered abilities have a trigger condition and an effect."</item>
///   <item>CR 400.7: "If an object in the exile zone or a face-up object in the command zone
///   is put into another zone, it ceases to exist in its current zone and enters the new zone."
///   (zone-change for the hand-to-battlefield move)</item>
/// </list>
///
/// <para>
/// ANCHORED (^…$): the full sentence is matched exactly to prevent this rule from
/// firing as a substring of any more-specific sibling. Priority 97 — higher than the
/// plain <see cref="DrawCardsTriggeredRule"/> (default 50) and
/// <see cref="PutFromHandOntoBattlefieldTriggeredRule"/> (63) so neither grabs a
/// portion of this composite sentence before the full form is tried.
/// </para>
/// </summary>
[TriggeredRule(Priority = 97)]
public sealed class DrawThenMayPutLandFromHandRule : ITriggeredRule
{
  // Anchored pattern: "draw a card, then you may put a[n] <filter> card from your hand onto the battlefield"
  // Groups:
  //   filter: the qualifier between "a[n] " and " card from your hand"
  private static readonly Regex _pattern = new(
    @"^draw\s+a\s+card,\s*then\s+you\s+may\s+put\s+a(?:n)?\s+(?<filter>.+?)\s+card\s+from\s+your\s+hand\s+onto\s+the\s+battlefield\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known card types for filter parsing (matching PutFromHandOntoBattlefieldTriggeredRule convention).
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var filterRaw = m.Groups["filter"].Value.Trim();
    var filter = BuildFilter(filterRaw);
    if (filter is null)
    {
      return false;
    }

    var drawOne = new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(1),
      Player = ObjectReference.You(),
    };

    var putFromHand = new PutFromHandOntoBattlefieldEffect
    {
      Filter = filter,
      Tapped = false,
    };

    var optional = new OptionalEffect { Inner = putFromHand };

    effect = new CompositeEffect { Effects = [drawOne, optional] };
    return true;
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> from the qualifier phrase between
  /// "a[n] " and " card from your hand". Handles bare card type and
  /// "subtype cardtype" forms.
  /// </summary>
  private static ObjectFilter? BuildFilter(string qualifier)
  {
    if (string.IsNullOrWhiteSpace(qualifier))
    {
      return null;
    }

    var trimmed = qualifier.Trim();
    var lastSpaceIdx = trimmed.LastIndexOf(' ');

    string cardTypePart;
    string? subtypePart;

    if (lastSpaceIdx >= 0)
    {
      cardTypePart = trimmed[(lastSpaceIdx + 1)..];
      subtypePart = trimmed[..lastSpaceIdx].Trim();
    }
    else
    {
      cardTypePart = trimmed;
      subtypePart = null;
    }

    if (!_knownCardTypes.Contains(cardTypePart))
    {
      // Treat entire qualifier as a subtype.
      return new ObjectFilter
      {
        Subtypes = [qualifier],
        Zone = Zone.Hand,
        Controller = ControllerFilter.You,
      };
    }

    List<string>? subtypes = null;
    if (!string.IsNullOrWhiteSpace(subtypePart))
    {
      subtypes = [subtypePart];
    }

    return new ObjectFilter
    {
      CardTypes = [cardTypePart.ToLowerInvariant()],
      Subtypes = subtypes is { Count: > 0 } ? subtypes : null,
      Zone = Zone.Hand,
      Controller = ControllerFilter.You,
    };
  }
}
