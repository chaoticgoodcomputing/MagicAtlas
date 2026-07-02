namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may search your library for a[n] &lt;filter&gt; card, put it onto the battlefield,
/// then shuffle." — optional triggered tutor directly to the battlefield.
///
/// <para>Handles the pattern found on Godo, Bandit Warlord and similar ETB tutors that
/// place the found card directly onto the battlefield (not into the hand or top of
/// library). The "you may" prefix is optional; its presence wraps the effect in
/// <see cref="OptionalEffect"/>.</para>
///
/// <para>Maps to a <see cref="CompositeEffect"/> containing:</para>
/// <list type="number">
///   <item><see cref="SearchLibraryEffect"/> with Destination = <see cref="SearchDestination.Battlefield"/> (or <see cref="SearchDestination.BattlefieldTapped"/> when "tapped" is present).</item>
///   <item><see cref="ShuffleEffect"/> for the mandatory library shuffle.</item>
/// </list>
///
/// <para>CR 701.23 (Search); CR 701.26 (Tap and Untap for tapped variant).</para>
///
/// <para>Priority 62 — above the generic Hand-destination rule (Priority 60) so this
/// battlefield-specific rule is tried before the hand rule, and the two cannot
/// cross-match because their destination clauses are anchored differently.</para>
/// </summary>
[TriggeredRule(Priority = 62)]
public sealed class SearchLibraryToBattlefieldTriggeredRule : ITriggeredRule
{
  // Matches:
  //   [you may ]search your library for a[n] <filter> card[,] put it onto the battlefield[tapped][,] then shuffle
  // The <filter> group captures the qualifier between "for a[n] " and " card".
  // Optional "you may" prefix, optional "tapped" qualifier.
  private static readonly Regex _pattern = new(
    @"^(?:you\s+may\s+)?search\s+your\s+library\s+for\s+a(?:n)?\s+"
    + @"(?<filter>[^,]+?)\s+card"
    + @",\s*put\s+it\s+onto\s+the\s+battlefield(?:\s+(?<tapped>tapped))?,?\s*then\s+shuffle\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG card types (singular, lowercase).
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  // MTG supertypes (CR 205.4).
  private static readonly HashSet<string> _knownSupertypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "basic", "legendary", "snow", "world", "ongoing",
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

    var isOptional = trimmed.StartsWith("you may", StringComparison.OrdinalIgnoreCase);
    var filterRaw = m.Groups["filter"].Value.Trim();
    var isTapped = m.Groups["tapped"].Success;

    // "basic land" searches are owned by SearchBasicLandTriggeredRule (Priority 50). This rule is
    // Priority 62 (it runs first), so without this guard it would preempt the basic-land sibling for
    // the ~15 basic-land ETB tutors (Farhaven Elf, Wild Wanderer, …) and silently reshape their AST.
    // Returning false here lets dispatch fall through to the dedicated basic-land rule.
    if (filterRaw.StartsWith("basic ", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    var filter = BuildFilter(filterRaw);
    if (filter is null)
    {
      return false;
    }

    var destination = isTapped ? SearchDestination.BattlefieldTapped : SearchDestination.Battlefield;

    var composite = new CompositeEffect
    {
      Effects =
      [
        new SearchLibraryEffect
        {
          Filter = filter,
          Count = LiteralQuantity.Of(1),
          Destination = destination,
          Revealed = false,
        },
        new ShuffleEffect
        {
          Player = ObjectReference.You(),
        },
      ],
    };

    effect = isOptional ? new OptionalEffect { Inner = composite } : composite;
    return true;
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> from the qualifier phrase between
  /// "for a[n] " and " card" in the oracle text.
  ///
  /// <list type="bullet">
  ///   <item>Known card type only → <c>CardTypes = [type]</c></item>
  ///   <item>Known supertype + card type → <c>Supertypes = [super]</c>, <c>CardTypes = [type]</c></item>
  ///   <item>Subtype + card type → <c>Subtypes = [sub]</c>, <c>CardTypes = [type]</c></item>
  ///   <item>Bare unrecognised word → <c>Subtypes = [word]</c> (tribal / Equipment / Aura / etc.)</item>
  /// </list>
  /// </summary>
  private ObjectFilter? BuildFilter(string qual)
  {
    if (string.IsNullOrWhiteSpace(qual))
    {
      return null;
    }

    var parts = qual.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0)
    {
      return null;
    }

    var last = parts[^1];

    if (_knownCardTypes.Contains(last) && parts.Length > 1)
    {
      var supertypes = new List<string>();
      var subtypes = new List<string>();
      foreach (var w in parts[..^1])
      {
        var titled = char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant();
        (_knownSupertypes.Contains(w) ? supertypes : subtypes).Add(titled);
      }
      return new ObjectFilter
      {
        CardTypes = [last.ToLowerInvariant()],
        Supertypes = supertypes.Count > 0 ? supertypes : null,
        Subtypes = subtypes.Count > 0 ? subtypes : null,
      };
    }

    if (_knownCardTypes.Contains(last))
    {
      // Single known card type, e.g. "creature", "artifact"
      return new ObjectFilter { CardTypes = [last.ToLowerInvariant()] };
    }

    // Treat entire qualifier as a subtype name (e.g. "Equipment", "Rebel", "Samurai")
    var subtype = char.ToUpperInvariant(qual[0]) + qual[1..].ToLowerInvariant();
    return new ObjectFilter { Subtypes = [subtype] };
  }
}
