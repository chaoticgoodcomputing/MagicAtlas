namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may search your library for a [filter] card, reveal it, put it into your hand, then shuffle."
///
/// Handles the non-basic-land ETB tutor pattern (Rule 701.23: Search).
/// Supports three filter shapes:
///   1. Plain card type — "a creature card", "an artifact card", etc.
///   2. Subtype (tribal) — "an Elf card", "a Goblin card", etc.
///   3. Card type + mana value comparison — "a creature card with mana value 6 or greater"
///
/// The "you may" prefix is optional; its presence sets IsOptional=true.
/// Only the Hand destination is covered here (reveal → hand → shuffle).
/// The SearchBasicLandTriggeredRule handles basic-land searches at higher priority.
///
/// Priority 60 (above default 50) so this is tried before generic fallback rules
/// but after any higher-priority specialised rules.
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class SearchLibraryToHandTriggeredRule : ITriggeredRule
{
  // Matches "you may search your library for a[n] [filter], reveal it, put it into your hand, then shuffle"
  // The [filter] group captures the full qualifier between "for a[n] " and " card".
  // Handles optional "you may" prefix.
  private static readonly Regex _pattern = new(
    @"^(?:you\s+may\s+)?search\s+your\s+library\s+for\s+a(?:n)?\s+"
    + @"(?<filter>[^,]+?)\s+card"
    + @"(?:\s+(?<mv>with\s+mana\s+value\s+(?<op>\d+\s+or\s+(?:less|greater)|equal\s+to\s+\d+|\d+)))?"
    + @",\s*reveal\s+it,\s*put\s+it\s+into\s+your\s+hand,\s*then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Mana-value comparison phrase embedded inside the filter group when the card
  // type word precedes the "with mana value N" qualifier on the same fragment.
  // e.g. "creature card with mana value 6 or greater" — the outer regex already
  // handles this via the <mv> group, but we also need to strip any trailing
  // mana-value clause that was absorbed into <filter> for cards that phrase it
  // differently (e.g., "creature card with mana value 6 or greater" where the
  // outer pattern's <filter> stops at the first space before "with").
  private static readonly Regex _mvInFilter = new(
    @"^(?<type>[a-z]+(?:\s+[a-z]+)?)\s+with\s+mana\s+value\s+(?<mv>.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG card types (singular, lowercase).
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // Defer to SearchBasicLandTriggeredRule for "basic land card" patterns.
    // That rule fires at Priority 50; bailing here ensures it retains ownership
    // regardless of dispatch order.
    if (text.Contains("basic land", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var isOptional = text.TrimStart().StartsWith("you may", StringComparison.OrdinalIgnoreCase);
    var filterRaw = m.Groups["filter"].Value.Trim();

    // Attempt to parse a mana-value comparison embedded in the filter fragment
    // (e.g. "creature with mana value 6 or greater") — this happens when the
    // <mv> outer group didn't capture it because the qualifier sits inside the
    // filter phrase.
    Comparison? mvComparison = null;
    var mvInFilterMatch = _mvInFilter.Match(filterRaw);
    if (mvInFilterMatch.Success)
    {
      filterRaw = mvInFilterMatch.Groups["type"].Value.Trim();
      mvComparison = ParseManaValueComparison(mvInFilterMatch.Groups["mv"].Value.Trim());
    }
    else if (m.Groups["mv"].Success)
    {
      // Outer regex captured a trailing "with mana value N op" group.
      mvComparison = ParseManaValueComparison(
        m.Groups["mv"].Value
          .Replace("with mana value ", "", StringComparison.OrdinalIgnoreCase)
          .Trim()
      );
    }

    // Build the ObjectFilter. Three shapes:
    //   (a) Known card type only  → CardTypes = [type]
    //   (b) Unknown word          → Subtypes  = [word]  (tribal tutor)
    //   (c) Either, plus MV comparison
    ObjectFilter filter;
    if (_knownCardTypes.Contains(filterRaw))
    {
      filter = new ObjectFilter
      {
        CardTypes = [filterRaw.ToLowerInvariant()],
        ManaValueComparison = mvComparison,
      };
    }
    else
    {
      // Treat as a subtype (tribal) search.
      // Title-case the word to match oracle convention (e.g. "Elf", "Goblin").
      var subtype = char.ToUpperInvariant(filterRaw[0]) + filterRaw[1..].ToLowerInvariant();
      filter = new ObjectFilter
      {
        Subtypes = [subtype],
        ManaValueComparison = mvComparison,
      };
    }

    effect = new SearchLibraryEffect
    {
      Filter = filter,
      Count = LiteralQuantity.Of(1),
      Destination = SearchDestination.Hand,
      Revealed = true,
      IsOptional = isOptional,
    };
    return true;
  }

  /// <summary>
  /// Parses "N or greater", "N or less", "equal to N", or bare "N" into a
  /// <see cref="Comparison"/>. Returns <see langword="null"/> when the text is
  /// not recognisable.
  /// </summary>
  private static Comparison? ParseManaValueComparison(string mvText)
  {
    mvText = mvText.Trim();

    // "N or greater" / "N or more"
    var gtMatch = Regex.Match(mvText, @"^(\d+)\s+or\s+(?:greater|more)$", RegexOptions.IgnoreCase);
    if (gtMatch.Success && int.TryParse(gtMatch.Groups[1].Value, out var gtVal))
    {
      return new Comparison { Operator = ComparisonOperator.GreaterThanOrEqual, Value = gtVal };
    }

    // "N or less" / "N or fewer"
    var ltMatch = Regex.Match(mvText, @"^(\d+)\s+or\s+(?:less|fewer)$", RegexOptions.IgnoreCase);
    if (ltMatch.Success && int.TryParse(ltMatch.Groups[1].Value, out var ltVal))
    {
      return new Comparison { Operator = ComparisonOperator.LessThanOrEqual, Value = ltVal };
    }

    // "equal to N"
    var eqMatch = Regex.Match(mvText, @"^equal\s+to\s+(\d+)$", RegexOptions.IgnoreCase);
    if (eqMatch.Success && int.TryParse(eqMatch.Groups[1].Value, out var eqVal))
    {
      return new Comparison { Operator = ComparisonOperator.Equal, Value = eqVal };
    }

    // bare "N"
    if (int.TryParse(mvText, out var bareVal))
    {
      return new Comparison { Operator = ComparisonOperator.Equal, Value = bareVal };
    }

    return null;
  }
}
