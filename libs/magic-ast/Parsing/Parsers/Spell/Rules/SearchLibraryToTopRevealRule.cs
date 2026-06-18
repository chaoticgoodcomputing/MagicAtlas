namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Spell-resolution rule for the Mystical Tutor pattern:
/// "Search your library for an instant or sorcery card, reveal it, then shuffle and put that card on top."
///
/// Handles:
/// <list type="bullet">
///   <item>"an instant or sorcery card" — two-type disjunction (Filter.CardTypes = ["instant", "sorcery"])</item>
///   <item>"a[n] [type] card" — single typed filter, e.g. "an artifact card", "a creature card"
///   (type word parsed into CardTypes or Supertypes)</item>
/// </list>
///
/// All variants map to a single <see cref="SearchLibraryEffect"/> with
/// <c>Destination = TopOfLibrary</c> and <c>Revealed = true</c>.
/// The reveal, shuffle, and put-on-top clauses are folded into the effect
/// per the convention established by <see cref="SearchLibraryToTopRule"/> and
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.SearchLibraryToTopTriggeredRule"/>.
/// Rule 701.23 (Search). Rule 701.20 (Reveal).
///
/// Priority 60 — fires before <see cref="SearchLibraryToTopRule"/> (priority 50) because
/// this pattern includes "reveal it," which is absent from the plain Vampiric Tutor form;
/// both rules are fully anchored (^…$) so there is no substring collision between them.
/// </summary>
[SpellRule(Priority = 60)]
public sealed class SearchLibraryToTopRevealRule : ISpellRule
{
  // Matches the two-type "X or Y" disjunction form:
  //   "Search your library for an instant or sorcery card, reveal it, then shuffle and put that card on top"
  // The (?<type1>[A-Za-z]+)\s+or\s+(?<type2>[A-Za-z]+) group captures the disjunction.
  private static readonly Regex _orPattern = new(
    @"^Search\s+your\s+library\s+for\s+an?\s+(?<type1>[A-Za-z]+)\s+or\s+(?<type2>[A-Za-z]+)\s+card,\s*"
    + @"reveal\s+it,\s*then\s+shuffle\s+and\s+put\s+that\s+card\s+on\s+top$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the single-type form with reveal:
  //   "Search your library for a[n] [filter] card, reveal it, then shuffle and put that card on top"
  private static readonly Regex _singlePattern = new(
    @"^Search\s+your\s+library\s+for\s+an?\s+(?<filter>(?:[A-Za-z]+\s+)+)?card,\s*"
    + @"reveal\s+it,\s*then\s+shuffle\s+and\s+put\s+that\s+card\s+on\s+top$",
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

    // Try the "X or Y" disjunction form first (e.g. Mystical Tutor).
    var orMatch = _orPattern.Match(trimmed);
    if (orMatch.Success)
    {
      var type1 = orMatch.Groups["type1"].Value.ToLowerInvariant();
      var type2 = orMatch.Groups["type2"].Value.ToLowerInvariant();

      // Both tokens must be known card types for this pattern to own the match;
      // unknown words fall through to the UnparsedAbility path so they aren't silently mislabelled.
      if (!_cardTypes.Contains(type1) || !_cardTypes.Contains(type2))
      {
        return false;
      }

      effect = new SearchLibraryEffect
      {
        Filter = new ObjectFilter { CardTypes = [type1, type2] },
        Count = LiteralQuantity.Of(1),
        Destination = SearchDestination.TopOfLibrary,
        Revealed = true,
      };
      return true;
    }

    // Try the single-type form with reveal.
    var singleMatch = _singlePattern.Match(trimmed);
    if (!singleMatch.Success)
    {
      return false;
    }

    var filterGroup = singleMatch.Groups["filter"].Value.Trim();
    var filter = BuildFilter(filterGroup);

    effect = new SearchLibraryEffect
    {
      Filter = filter,
      Count = LiteralQuantity.Of(1),
      Destination = SearchDestination.TopOfLibrary,
      Revealed = true,
    };
    return true;
  }

  /// <summary>
  /// Parses the optional qualifier words before "card" into an
  /// <see cref="ObjectFilter"/>.
  /// Empty string → unfiltered card filter.
  /// One or more words → partitioned into Supertypes and CardTypes (in that order).
  /// Any word not in either table is treated as a subtype (e.g. "Goblin").
  /// Mirrors <see cref="SearchLibraryToTopRule"/>.
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
        supertypes.Add(token[0].ToString().ToUpperInvariant() + token[1..].ToLowerInvariant());
      }
      else if (_cardTypes.Contains(token))
      {
        cardTypes.Add(token.ToLowerInvariant());
      }
      else
      {
        subtypes.Add(char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant());
      }
    }

    var finalCardTypes = cardTypes.Count > 0 ? cardTypes : new List<string> { "card" };

    return new ObjectFilter
    {
      Supertypes = supertypes.Count > 0 ? supertypes : null,
      CardTypes = finalCardTypes,
      Subtypes = subtypes.Count > 0 ? subtypes : null,
    };
  }
}
