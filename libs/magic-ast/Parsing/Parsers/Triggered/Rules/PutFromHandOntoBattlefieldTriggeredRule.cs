namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "you may put a[n] &lt;filter&gt; card from your hand onto the battlefield tapped and attacking that opponent"
///
/// <para>
/// Handles the Kaalia of the Vast pattern: an optional zone-change from hand to
/// battlefield where the card enters tapped and attacking the opponent named by the
/// enclosing trigger. The "you may" wrapper is materialised as
/// <see cref="OptionalEffect"/> around the inner <see cref="PutFromHandOntoBattlefieldEffect"/>.
/// </para>
///
/// <para>Rule citations:</para>
/// <list type="bullet">
///   <item>CR 508.1b — "attacks an opponent" trigger; the card enters attacking that
///   same opponent.</item>
///   <item>CR 400.7 — zone change creates a new object.</item>
///   <item>CR 302.6 / 110.6b — permanent enters tapped.</item>
///   <item>CR 603.1 — triggered ability trigger + effect.</item>
/// </list>
///
/// <para>
/// Priority 63 — above the generic battlefield-search rules and sibling hand-to-play
/// rules so the "from your hand onto the battlefield" form is matched before any
/// library-search fallback. The pattern is fully anchored (^...$) so it cannot
/// match as a substring of a more-complex sibling effect.
/// </para>
/// </summary>
[TriggeredRule(Priority = 63)]
public sealed class PutFromHandOntoBattlefieldTriggeredRule : ITriggeredRule
{
  // Anchored pattern:
  //   [you may ]put a[n] <filter> card from your hand onto the battlefield[tapped[and attacking that opponent]]
  // Groups:
  //   optional: presence of "you may " prefix
  //   filter:   qualifier between "a[n] " and " card"
  //   tapped:   presence of "tapped" qualifier
  //   attacking: presence of "attacking that opponent"
  private static readonly Regex _pattern = new(
    @"^(?<optional>you\s+may\s+)?put\s+a(?:n)?\s+(?<filter>.+?)\s+card\s+from\s+your\s+hand\s+onto\s+the\s+battlefield"
    + @"(?:\s+(?<tapped>tapped)(?:\s+and\s+(?<attacking>attacking\s+that\s+opponent))?)?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG card types (singular, lowercase) for filter parsing.
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

    var isOptional = m.Groups["optional"].Success;
    var filterRaw = m.Groups["filter"].Value.Trim();
    var isTapped = m.Groups["tapped"].Success;
    var isAttackingThatOpponent = m.Groups["attacking"].Success ? (bool?)true : null;

    var filter = BuildFilter(filterRaw);
    if (filter is null)
    {
      return false;
    }

    var inner = new PutFromHandOntoBattlefieldEffect
    {
      Filter = filter,
      Tapped = isTapped,
      AttackingThatOpponent = isAttackingThatOpponent,
    };

    effect = isOptional ? new OptionalEffect { Inner = inner } : inner;
    return true;
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> from the qualifier phrase between
  /// "a[n] " and " card". Handles:
  /// <list type="bullet">
  ///   <item>Single card type: "creature" → CardTypes:[creature], Zone:Hand, Controller:You</item>
  ///   <item>Disjunction of subtypes + card type: "Angel, Demon, or Dragon creature"
  ///   → Subtypes:[Angel, Demon, Dragon], CardTypes:[creature], Zone:Hand, Controller:You</item>
  /// </list>
  /// </summary>
  private static ObjectFilter? BuildFilter(string qualifier)
  {
    if (string.IsNullOrWhiteSpace(qualifier))
    {
      return null;
    }

    var trimmed = qualifier.Trim();

    // Check if the last token is a known card type.
    // Pattern: "[subtype1, subtype2, or subtype3 ]cardtype"
    // e.g. "Angel, Demon, or Dragon creature"
    //      "creature" (bare)
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
      // Treat entire qualifier as a subtype (e.g. "Ninja" → Subtypes:[Ninja])
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
      subtypes = ParseDisjunction(subtypePart);
      if (subtypes is null || subtypes.Count == 0)
      {
        return null;
      }
    }

    return new ObjectFilter
    {
      CardTypes = [cardTypePart.ToLowerInvariant()],
      Subtypes = subtypes is { Count: > 0 } ? subtypes : null,
      Zone = Zone.Hand,
      Controller = ControllerFilter.You,
    };
  }

  /// <summary>
  /// Parses a comma-separated "or"-joined list of subtype words into a list.
  /// "Angel, Demon, or Dragon" → ["Angel", "Demon", "Dragon"]
  /// "Angel" → ["Angel"]
  /// </summary>
  private static List<string>? ParseDisjunction(string raw)
  {
    if (string.IsNullOrWhiteSpace(raw))
    {
      return null;
    }

    // Split on ", " and " or " separators (handles "A, B, or C" and "A or B").
    var parts = Regex.Split(raw, @",\s*(?:or\s+)?|\s+or\s+")
      .Select(p => p.Trim().TrimEnd(','))
      .Where(p => !string.IsNullOrWhiteSpace(p) && !p.Equals("or", StringComparison.OrdinalIgnoreCase))
      .Select(p => char.ToUpperInvariant(p[0]) + p[1..])
      .ToList();

    return parts.Count > 0 ? parts : null;
  }
}
