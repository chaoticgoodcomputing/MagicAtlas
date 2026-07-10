namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "mill N cards, then you may return [type-disjunction] card from your graveyard to your
/// hand." — the single-sentence "mill, then optionally reanimate to HAND" composite
/// (Overlord of the Balemurk). Distinct from the reflexive "mill N cards. When you do,
/// return target … to the BATTLEFIELD" shape
/// (<see cref="ReflexiveMillThenReturnCreatureFromGraveyardRule"/>): here the two actions
/// happen in one resolution ("…, then you may…", CR 608.2 sequencing), not as a reflexive
/// delayed trigger (CR 603.12), and the return destination is the hand.
///
/// <para>
/// CR 701.17a (Mill): "For a player to mill a number of cards, that player puts that many
/// cards from the top of their library into their graveyard." CR 118.12 ("you may …"):
/// the optional return is wrapped in an <see cref="OptionalEffect"/>. The return card is an
/// indefinite reference ("a … card", not "target") → <see cref="ObjectReferenceKind.Any"/>
/// (CR 115.1 — only "target" creates a target).
/// </para>
///
/// <para>
/// The return filter is a card-type disjunction ("a non-Avatar creature card or a
/// planeswalker card", CR 115.3 OR semantics) with an optional "non-[subtype]" exclusion
/// captured on <see cref="ObjectFilter.ExcludedSubtypes"/> and the source zone/controller
/// on <see cref="ObjectFilter.Zone"/> / <see cref="ObjectFilter.Controller"/> ("from your
/// graveyard").
/// </para>
///
/// <para>
/// ANCHORED (^…$): the whole "mill … then you may return … from your graveyard to your
/// hand" sentence is anchored, so it cannot substring-match into a more specific sibling.
/// Priority 990 places it above the broad, unanchored <see cref="ReturnToHandRule"/> and
/// <see cref="MillTriggeredRule"/> (default priority) so the whole composite is captured in
/// one place rather than either half being parsed in isolation (which drops the other).
/// </para>
/// </summary>
[TriggeredRule(Priority = 990)]
public sealed class MillThenOptionalReturnFromGraveyardToHandRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^mill\s+(?<count>\w+)\s+cards?,\s*then\s+you\s+may\s+return\s+(?<filter>.+?)\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // One disjunct: "a non-Avatar creature card" / "a planeswalker card".
  private static readonly Regex _disjunct = new(
    @"^(?:a|an)\s+(?:non-(?<excl>[A-Za-z]+)\s+)?(?<type>[A-Za-z]+)\s+card$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "artifact", "creature", "enchantment", "land", "permanent", "planeswalker",
    "instant", "sorcery", "battle",
  };

  private static readonly IReadOnlyDictionary<string, int> _numberWords =
    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
      ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
      ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim().TrimEnd('.').Trim());
    if (!m.Success)
    {
      return false;
    }

    var countRaw = m.Groups["count"].Value;
    int count;
    if (_numberWords.TryGetValue(countRaw, out var cw))
    {
      count = cw;
    }
    else if (!int.TryParse(countRaw, out count))
    {
      return false;
    }

    // Parse the "[disjunct] or [disjunct] or …" return filter.
    var cardTypes = new List<string>();
    var excludedSubtypes = new List<string>();
    foreach (var part in Regex.Split(m.Groups["filter"].Value.Trim(), @"\s+or\s+"))
    {
      var dm = _disjunct.Match(part.Trim());
      if (!dm.Success)
      {
        return false;
      }

      var type = dm.Groups["type"].Value.ToLowerInvariant();
      if (!_knownCardTypes.Contains(type))
      {
        return false;
      }

      if (!cardTypes.Contains(type))
      {
        cardTypes.Add(type);
      }

      if (dm.Groups["excl"].Success)
      {
        // Preserve the printed subtype casing (e.g. "Avatar").
        var excl = dm.Groups["excl"].Value;
        if (!excludedSubtypes.Contains(excl))
        {
          excludedSubtypes.Add(excl);
        }
      }
    }

    if (cardTypes.Count == 0)
    {
      return false;
    }

    var returnTarget = new ObjectReference
    {
      Kind = ObjectReferenceKind.Any,
      Filter = new ObjectFilter
      {
        CardTypes = cardTypes,
        ExcludedSubtypes = excludedSubtypes.Count > 0 ? excludedSubtypes : null,
        Zone = Zone.Graveyard,
        Controller = ControllerFilter.You,
      },
    };

    effect = new CompositeEffect
    {
      Effects =
      [
        new MillEffect
        {
          Count = LiteralQuantity.Of(count),
          Player = ObjectReference.You(),
        },
        new OptionalEffect
        {
          Inner = new ReturnToHandEffect { Target = returnTarget },
        },
      ],
    };
    return true;
  }
}
