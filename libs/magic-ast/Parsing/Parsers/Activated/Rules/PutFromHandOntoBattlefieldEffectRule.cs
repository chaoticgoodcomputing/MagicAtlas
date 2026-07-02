namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "[You may ]put a[n] &lt;filter&gt; card from your hand onto the battlefield[ tapped[ and attacking that opponent]]."
///
/// <para>
/// Handles the Sakura-Tribe Scout / Kodama of the East Tree pattern reached via an
/// activated (rather than triggered) ability: an optional zone-change that moves a
/// card directly from the controller's hand to the battlefield. Per CR 305.4,
/// "putting" a land onto the battlefield this way is explicitly NOT "playing a land"
/// and does not count against the turn's land-play limit (CR 116.2a describes the
/// distinct "play a land" special action this effect is NOT).
/// </para>
///
/// <para>Rule citations:</para>
/// <list type="bullet">
///   <item>CR 305.4 — "Effects may also allow players to 'put' lands onto the
///   battlefield. This isn't the same as 'playing a land' and doesn't count as a
///   land played during the current turn."</item>
///   <item>CR 116.2a — "Playing a land is a special action. To play a land, a
///   player puts that land onto the battlefield from the zone it was in (usually
///   that player's hand). By default, a player can take this action only once
///   during each of their turns. A player can take this action any time they have
///   priority and the stack is empty during a main phase of their turn. See rule
///   305, 'Lands.'"</item>
///   <item>CR 602.1a — the activation cost / effect split at the colon.</item>
/// </list>
///
/// <para>
/// Priority 64 — below <see cref="SearchLibraryToBattlefieldEffectRule"/> (65) and
/// above the generic fallback rules. The pattern is fully anchored (^...$) so it
/// cannot match as a substring of a more-complex sibling effect. Duplicated (rather
/// than shared) from the triggered-ability sibling
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.PutFromHandOntoBattlefieldTriggeredRule"/>
/// because that rule implements a different interface (<c>ITriggeredRule</c>) and
/// sharing would require touching a hot shared file.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 64)]
public sealed class PutFromHandOntoBattlefieldEffectRule : IActivatedEffectRule
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

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var isOptional = m.Groups["optional"].Success;
    var filterRaw = m.Groups["filter"].Value.Trim();
    var isTapped = m.Groups["tapped"].Success;
    var isAttackingThatOpponent = m.Groups["attacking"].Success ? (bool?)true : null;

    var filter = BuildFilter(filterRaw);
    if (filter is null)
    {
      return null;
    }

    var inner = new PutFromHandOntoBattlefieldEffect
    {
      Filter = filter,
      Tapped = isTapped,
      AttackingThatOpponent = isAttackingThatOpponent,
    };

    return isOptional ? new OptionalEffect { Inner = inner } : inner;
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> from the qualifier phrase between
  /// "a[n] " and " card". Handles:
  /// <list type="bullet">
  ///   <item>Single card type: "land" → CardTypes:[land], Zone:Hand, Controller:You</item>
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
    //      "land" (bare)
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
