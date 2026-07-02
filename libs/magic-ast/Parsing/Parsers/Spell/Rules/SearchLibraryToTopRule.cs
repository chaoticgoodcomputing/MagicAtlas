namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Spell-resolution rule for the Vampiric Tutor pattern:
/// "Search your library for a [type] card, then shuffle and put that card on top."
///
/// Handles:
/// <list type="bullet">
///   <item>"a card" — any card (Filter.CardTypes = ["card"])</item>
///   <item>"a [type] card" — typed filter, e.g. "a creature card",
///   "a basic land card" (type word(s) parsed into CardTypes / Supertypes)</item>
/// </list>
///
/// All variants map to a single <see cref="SearchLibraryEffect"/> with
/// <c>Destination = TopOfLibrary</c>. The shuffle and put-on-top clauses are
/// folded into the effect per the convention established by
/// <see cref="SearchLibraryToHandRule"/> and <see cref="SearchLibraryToBattlefieldRule"/>.
/// Rule 701.23 (Search).
/// </summary>
[SpellRule]
public sealed class SearchLibraryToTopRule : ISpellRule
{
  // Captures an optional sequence of type qualifiers before "card":
  //   "a card"                 → filter = null (any card)
  //   "a creature card"        → filter = "creature"
  //   "a basic land card"      → filter = "basic land"
  private static readonly Regex _pattern = new(
    @"^Search\s+your\s+library\s+for\s+a\s+(?<filter>(?:[A-Za-z]+\s+)+)?card,\s*"
    + @"then\s+shuffle\s+and\s+put\s+that\s+card\s+on\s+top$",
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
    var m = _pattern.Match(text.Trim().TrimEnd('.'));
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
      Destination = SearchDestination.TopOfLibrary,
      Revealed = false,
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
        supertypes.Add(token[0].ToString().ToUpperInvariant() + token[1..].ToLowerInvariant());
      }
      else if (_cardTypes.Contains(token))
      {
        cardTypes.Add(token.ToLowerInvariant());
      }
      else
      {
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
