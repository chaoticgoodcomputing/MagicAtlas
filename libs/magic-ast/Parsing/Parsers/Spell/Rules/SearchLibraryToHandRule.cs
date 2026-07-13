namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Spell-resolution rule for the tutor pattern:
/// "Search your library for a[n] [type] card, [reveal it,] put (that card|it) into your hand, then shuffle."
///
/// Handles:
/// <list type="bullet">
///   <item>"a card" — any card (Filter.CardTypes = ["card"])</item>
///   <item>"a [type] card" / "an [type] card" — typed filter, e.g. "a creature card",
///   "a basic land card" (type word(s) parsed into CardTypes / Supertypes); the
///   article is "a" or "an" per English grammar depending on the following word
///   (e.g. Wild Idea's "an instant or sorcery card").</item>
///   <item>"an [type1] or [type2] card" — two-type disjunction (Filter.CardTypes =
///   [type1, type2]), the Wild Idea shape, mirroring
///   <see cref="SearchLibraryToTopRevealRule"/>'s Mystical Tutor disjunction arm.</item>
///   <item>an optional reveal clause ("reveal it," or "reveal that card,")
///   before the put-to-hand clause (e.g. Lay of the Land, Eladamri's Call) —
///   sets <c>Revealed = true</c> when present.</item>
/// </list>
///
/// All variants map to a single <see cref="SearchLibraryEffect"/> with
/// <c>Destination = Hand</c>. The optional reveal, put-to-hand, and shuffle
/// clauses are folded into the effect per the existing convention established by
/// <c>SearchBasicLandTriggeredRule</c> and the Solemn Simulacrum fixture.
/// Rule 701.23 (Search).
/// </summary>
[SpellRule]
public sealed class SearchLibraryToHandRule : ISpellRule
{
  // Matches the two-type "X or Y" disjunction form:
  //   "Search your library for an instant or sorcery card, reveal it, put it into your hand, then shuffle"
  private static readonly Regex _orPattern = new(
    @"^Search\s+your\s+library\s+for\s+an?\s+(?<type1>[A-Za-z]+)\s+or\s+(?<type2>[A-Za-z]+)\s+card,\s*"
    + @"(?<reveal>reveal\s+(?:it|that\s+card),\s*)?put\s+(?:that\s+card|it)\s+into\s+your\s+hand,\s*then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Captures an optional sequence of type qualifiers before "card":
  //   "a card"                 → filter = null (any card)
  //   "a creature card"        → filter = "creature"
  //   "a basic land card"      → filter = "basic land"
  //   "a basic Forest card"    → filter = "basic Forest"
  // The article is "a" or "an" (an? — Rule 701.23 has no grammar significance;
  // this is plain English usage that varies by the following word's sound).
  // The optional <reveal> group captures a "reveal it," / "reveal that card,"
  // clause sitting between the filter and the put-to-hand clause; its presence
  // sets Revealed = true.
  private static readonly Regex _pattern = new(
    @"^Search\s+your\s+library\s+for\s+an?\s+(?<filter>(?:[A-Za-z]+\s+)+)?card,\s*"
    + @"(?<reveal>reveal\s+(?:it|that\s+card),\s*)?put\s+(?:that\s+card|it)\s+into\s+your\s+hand,\s*then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Canonical supertypes recognised in the filter position.
  private static readonly HashSet<string> _supertypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "Basic", "Legendary", "Snow", "World",
  };

  // Canonical card types recognised in the filter position.
  private static readonly HashSet<string> _cardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "land", "artifact", "enchantment", "instant", "sorcery",
    "planeswalker", "battle", "tribal",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    // Try the "X or Y" disjunction form first (e.g. Wild Idea).
    var orMatch = _orPattern.Match(trimmed);
    if (orMatch.Success)
    {
      var type1 = orMatch.Groups["type1"].Value.ToLowerInvariant();
      var type2 = orMatch.Groups["type2"].Value.ToLowerInvariant();

      // Both tokens must be known card types for this pattern to own the match;
      // unknown words fall through (leaving the single-filter pattern below a chance,
      // then ultimately the unparsed fallback) so they aren't silently mislabelled.
      if (_cardTypes.Contains(type1) && _cardTypes.Contains(type2))
      {
        effect = new SearchLibraryEffect
        {
          Filter = new ObjectFilter { CardTypes = [type1, type2] },
          Count = LiteralQuantity.Of(1),
          Destination = SearchDestination.Hand,
          Revealed = orMatch.Groups["reveal"].Success,
        };
        return true;
      }
    }

    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var filterGroup = m.Groups["filter"].Value.Trim();
    var filter = BuildFilter(filterGroup);

    effect = new SearchLibraryEffect
    {
      Filter = filter,
      Count = LiteralQuantity.Of(1),
      Destination = SearchDestination.Hand,
      Revealed = m.Groups["reveal"].Success,
    };
    return true;
  }

  /// <summary>
  /// Parses the optional qualifier words before "card" into an
  /// <see cref="ObjectFilter"/>.
  /// Empty string → unfiltered card filter.
  /// One or more words → partitioned into Supertypes and CardTypes (in that order).
  /// Any word not in either table is treated as a subtype (e.g. "Forest", "Human").
  /// </summary>
  private static ObjectFilter BuildFilter(string filterPhrase)
  {
    if (string.IsNullOrWhiteSpace(filterPhrase))
    {
      return new ObjectFilter { CardTypes = ["card"] };
    }

    var tokens = filterPhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var supertypes = new List<string>();
    var cardTypes = new List<string>();
    var subtypes = new List<string>();

    foreach (var token in tokens)
    {
      if (_supertypes.Contains(token))
      {
        // Preserve original casing for supertypes (e.g. "Basic" not "basic").
        supertypes.Add(token[0].ToString().ToUpperInvariant() + token[1..].ToLowerInvariant());
      }
      else if (_cardTypes.Contains(token))
      {
        cardTypes.Add(token.ToLowerInvariant());
      }
      else
      {
        // Unknown token → subtype (e.g. "Forest", "Human", "Dragon").
        subtypes.Add(token);
      }
    }

    // If we have explicit card types, use them; otherwise fall back to generic "card".
    var finalCardTypes = cardTypes.Count > 0 ? cardTypes : new List<string> { "card" };

    return new ObjectFilter
    {
      Supertypes = supertypes.Count > 0 ? supertypes : null,
      CardTypes = finalCardTypes,
      Subtypes = subtypes.Count > 0 ? subtypes : null,
    };
  }
}
