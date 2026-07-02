namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may search your library for a [filter] card, reveal it, then shuffle and
/// put that card on top."
///
/// The ETB tutor-to-top pattern (the Lorwyn "Harbinger" cycle and kin), e.g.
/// Boggart Harbinger ("a Goblin card") and Dune Mover ("a basic land card").
/// Distinct from the existing ETB tutor rules
/// (<see cref="SearchLibraryToHandTriggeredRule"/>,
/// <see cref="SearchBasicLandTriggeredRule"/>) only in its destination: the found
/// card goes on top of the library (<see cref="SearchDestination.TopOfLibrary"/>)
/// rather than into the hand or onto the battlefield. The spell-resolution analogue
/// is <see cref="MagicAST.Parsing.Parsers.Spell.Rules.SearchLibraryToTopRule"/>.
/// Rule 701.23a (Search).
///
/// The "you may" prefix is optional; its presence sets IsOptional=true via
/// <see cref="MagicAST.AST.Effects.Core.EffectWrap.Optional"/>. The reveal clause
/// sets Revealed=true. The "shuffle and put that card on top" tail is folded into
/// the effect (Destination=TopOfLibrary) rather than modelled separately — the same
/// convention every sibling tutor rule uses for its shuffle clause.
///
/// The filter group is partitioned into Supertypes / CardTypes / Subtypes the same
/// way <see cref="MagicAST.Parsing.Parsers.Spell.Rules.SearchLibraryToTopRule"/>
/// partitions it, so "basic land" → Supertypes=[Basic] CardTypes=[land] and a bare
/// subtype word ("Goblin") → Subtypes=[Goblin].
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class SearchLibraryToTopTriggeredRule : ITriggeredRule
{
  // Captures an optional sequence of type qualifiers before "card":
  //   "a card"            → filter = null (any card)
  //   "a Goblin card"     → filter = "Goblin"
  //   "a basic land card" → filter = "basic land"
  // followed by the reveal + shuffle-and-put-on-top tail.
  private static readonly Regex _pattern = new(
    @"^(?:you\s+may\s+)?search\s+your\s+library\s+for\s+an?\s+"
    + @"(?<filter>(?:[A-Za-z]+\s+)+?)?card,\s*reveal\s+it,\s*"
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

    var isOptional = text.TrimStart().StartsWith("you may", StringComparison.OrdinalIgnoreCase);
    var filter = BuildFilter(m.Groups["filter"].Value.Trim());

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new SearchLibraryEffect
    {
      Filter = filter,
      Count = LiteralQuantity.Of(1),
      Destination = SearchDestination.TopOfLibrary,
      Revealed = true,
    }, isOptional);
    return true;
  }

  /// <summary>
  /// Parses the optional qualifier words before "card" into an
  /// <see cref="ObjectFilter"/>. Empty string → unfiltered card filter. One or
  /// more words → partitioned into Supertypes, CardTypes and Subtypes (any word
  /// in neither table is treated as a subtype, e.g. "Goblin"). Mirrors
  /// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.SearchLibraryToTopRule"/>.
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

    return new ObjectFilter
    {
      Supertypes = supertypes.Count > 0 ? supertypes : null,
      CardTypes = cardTypes.Count > 0 ? cardTypes : null,
      Subtypes = subtypes.Count > 0 ? subtypes : null,
    };
  }
}
