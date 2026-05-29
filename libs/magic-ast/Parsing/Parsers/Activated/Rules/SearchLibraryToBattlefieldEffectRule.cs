namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Search your library for a &lt;filter&gt; permanent card with mana value N or less,
/// put it onto the battlefield, then shuffle."
///
/// Covers the activated tutor-to-battlefield pattern (e.g. Amrou Scout, Lin Sivvi).
/// CR 701.23a: "To search for a card in a zone, look at all cards in that zone
/// (even if it's a hidden zone) and find a card that matches the given description."
///
/// Maps to a CompositeEffect containing:
///   1. SearchLibraryEffect (Destination = Battlefield) — the search and placement.
///   2. ShuffleEffect (Player = You) — the mandatory library shuffle.
///
/// The ObjectFilter carries:
///   - Subtypes: [subtype] when the qualifier is a creature-type (e.g. "Rebel")
///   - CardTypes: ["permanent"] when a card-type constraint is present
///   - ManaValueComparison: LessThanOrEqual N when "with mana value N or less"
/// </summary>
[ActivatedEffectRule(Priority = 65)]
public sealed class SearchLibraryToBattlefieldEffectRule : IActivatedEffectRule
{
  // Known MTG card types (singular, lowercase) that appear before "card" in oracle.
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  // MTG supertypes (CR 205.4). A qualifier word matching one of these belongs on
  // the Supertypes axis, NOT Subtypes — e.g. "basic land" → Supertypes=["Basic"].
  private static readonly HashSet<string> _knownSupertypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "basic", "legendary", "snow", "world", "ongoing",
  };

  // Matches: "Search your library for a[n] <qualifier> [card-type] card
  //           [with mana value N or less|greater], put it onto the battlefield[,] then shuffle."
  // The <qual> group captures everything between "for a[n] " and the final "card".
  private static readonly Regex _pattern = new(
    @"^search\s+your\s+library\s+for\s+a(?:n)?\s+"
    + @"(?<qual>.+?)\s+card"
    + @"(?:\s+with\s+mana\s+value\s+(?<mv>\d+\s+or\s+(?:less|greater|fewer|more)))?"
    + @",\s*put\s+it\s+onto\s+the\s+battlefield,?\s*then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Sub-pattern to extract an optional mana-value clause embedded inside <qual>:
  // e.g. "creature with mana value 3 or less" — although the outer pattern's <mv>
  // group handles the trailing case, we strip any residual from <qual>.
  private static readonly Regex _mvInQual = new(
    @"^(?<base>.+?)\s+with\s+mana\s+value\s+(?<mv>\d+\s+or\s+(?:less|greater|fewer|more))$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var qualRaw = m.Groups["qual"].Value.Trim();
    string? mvText = m.Groups["mv"].Success ? m.Groups["mv"].Value.Trim() : null;

    // If the outer <mv> group didn't capture but the qualifier itself contains
    // "with mana value N", pull it out of the qualifier string.
    if (mvText is null)
    {
      var inner = _mvInQual.Match(qualRaw);
      if (inner.Success)
      {
        qualRaw = inner.Groups["base"].Value.Trim();
        mvText = inner.Groups["mv"].Value.Trim();
      }
    }

    Comparison? mvComparison = mvText is not null ? ParseManaValueComparison(mvText) : null;

    // Build the filter. The qualifier may be:
    //   "Rebel permanent"   → Subtypes=["Rebel"], CardTypes=["permanent"]
    //   "creature"          → CardTypes=["creature"]
    //   "Rebel"             → Subtypes=["Rebel"]
    var filter = BuildFilter(qualRaw, mvComparison);

    var searchEffect = new SearchLibraryEffect
    {
      Filter = filter,
      Count = LiteralQuantity.Of(1),
      Destination = SearchDestination.Battlefield,
      Revealed = false,
      IsOptional = false,
    };

    var shuffleEffect = new ShuffleEffect
    {
      Player = ObjectReference.You(),
    };

    return new CompositeEffect
    {
      Effects = [searchEffect, shuffleEffect],
      IsOptional = false,
    };
  }

  /// <summary>
  /// Decomposes a raw qualifier like "Rebel permanent" or "creature" into an
  /// <see cref="ObjectFilter"/> with structured Subtypes / CardTypes axes.
  /// </summary>
  private static ObjectFilter BuildFilter(string qual, Comparison? mvComparison)
  {
    // Split on whitespace; if the last word is a known card type, treat the rest
    // as subtype words.
    var parts = qual.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0)
    {
      return new ObjectFilter { ManaValueComparison = mvComparison };
    }

    var last = parts[^1];
    if (_knownCardTypes.Contains(last) && parts.Length > 1)
    {
      // e.g. "Rebel permanent" → Subtypes=["Rebel"], CardTypes=["permanent"]
      //      "basic land"      → Supertypes=["Basic"], CardTypes=["land"]
      // Partition the pre-card-type words into supertypes vs subtypes (CR 205.4).
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
        ManaValueComparison = mvComparison,
      };
    }

    if (_knownCardTypes.Contains(last))
    {
      // Single known card type, e.g. "creature"
      return new ObjectFilter
      {
        CardTypes = [last.ToLowerInvariant()],
        ManaValueComparison = mvComparison,
      };
    }

    // Treat whole qualifier as a subtype/tribal name, e.g. "Rebel"
    var subtype = char.ToUpperInvariant(qual[0]) + qual[1..].ToLowerInvariant();
    return new ObjectFilter
    {
      Subtypes = [subtype],
      ManaValueComparison = mvComparison,
    };
  }

  /// <summary>
  /// Parses "N or less" / "N or greater" into a <see cref="Comparison"/>.
  /// Returns <see langword="null"/> when the text is not recognised.
  /// </summary>
  private static Comparison? ParseManaValueComparison(string mvText)
  {
    mvText = mvText.Trim();

    var ltMatch = Regex.Match(mvText, @"^(\d+)\s+or\s+(?:less|fewer)$", RegexOptions.IgnoreCase);
    if (ltMatch.Success && int.TryParse(ltMatch.Groups[1].Value, out var ltVal))
    {
      return new Comparison { Operator = ComparisonOperator.LessThanOrEqual, Value = ltVal };
    }

    var gtMatch = Regex.Match(mvText, @"^(\d+)\s+or\s+(?:greater|more)$", RegexOptions.IgnoreCase);
    if (gtMatch.Success && int.TryParse(gtMatch.Groups[1].Value, out var gtVal))
    {
      return new Comparison { Operator = ComparisonOperator.GreaterThanOrEqual, Value = gtVal };
    }

    if (int.TryParse(mvText, out var bareVal))
    {
      return new Comparison { Operator = ComparisonOperator.Equal, Value = bareVal };
    }

    return null;
  }
}
