namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Search your library for a(n) [Q1] or [Q2] card, put that card onto the
/// battlefield, then shuffle." — the activated tutor-to-battlefield pattern
/// whose eligibility clause is a two-way disjunction mixing a card-type word
/// and a capitalised creature-subtype word (Magda, Brazen Outlaw: "Search your
/// library for an artifact or Dragon card, put that card onto the battlefield,
/// then shuffle.").
///
/// Reuses the mixed type-or-subtype classification
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.CounterTargetTypeOrSubtypeSpellRule"/>
/// establishes and <see cref="Triggered.Rules.LookAtTopMayRevealMatchingRestToBottomTriggeredRule"/>
/// generalises: known card-type words route to <c>CardTypes</c>; capitalised
/// non-card-type words route to <c>Subtypes</c> (OR semantics — CR 205.3, a card
/// has zero or more subtypes, so it qualifies by matching EITHER member).
///
/// CR 701.23a: "To search for a card in a zone, look at all cards in that zone
/// (even if it's a hidden zone) and find a card that matches the given
/// description."
///
/// Maps to a <see cref="CompositeEffect"/> containing:
///   1. <see cref="SearchLibraryEffect"/> (Destination = Battlefield) — the search and placement.
///   2. <see cref="ShuffleEffect"/> (Player = You) — the mandatory library shuffle.
///
/// Distinct from the sibling <see cref="SearchLibraryToBattlefieldEffectRule"/>,
/// which anchors on the "put IT onto the battlefield" pronoun phrasing and
/// whose filter builder has no path for a mixed type-or-subtype disjunction;
/// this rule anchors on the "put THAT CARD onto the battlefield" phrasing
/// instead, so the two regexes are mutually exclusive by text and neither
/// shadows the other.
/// </summary>
[ActivatedEffectRule(Priority = 67)]
public sealed class SearchLibraryTypeOrSubtypeToBattlefieldEffectRule : IActivatedEffectRule
{
  // Known MTG card types (singular, lowercase). A disjunction member matching one
  // of these is a card type; anything else (capitalised, per oracle convention for
  // creature subtypes — CR 205.3m) is a subtype.
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  // Matches "Search your library for a(n) <Q1> or <Q2> card, put that card onto
  // the battlefield, then shuffle." Fully anchored (^…$).
  private static readonly Regex _pattern = new(
    @"^search\s+your\s+library\s+for\s+an?\s+(?<filter>[A-Za-z][A-Za-z ]*?\s+or\s+[A-Za-z][A-Za-z ]*?)\s+card,"
    + @"\s*put\s+that\s+card\s+onto\s+the\s+battlefield,\s*then\s+shuffle$",
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

    var members = m.Groups["filter"].Value
      .Split(" or ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (members.Length == 0)
    {
      return null;
    }

    var cardTypes = new List<string>();
    var subtypes = new List<string>();
    foreach (var member in members)
    {
      if (_knownCardTypes.Contains(member))
      {
        cardTypes.Add(member.ToLowerInvariant());
      }
      else if (char.IsUpper(member[0]))
      {
        subtypes.Add(member);
      }
      else
      {
        // Neither a known card type nor a capitalised subtype word — decline
        // rather than guess.
        return null;
      }
    }

    var searchEffect = new SearchLibraryEffect
    {
      Filter = new ObjectFilter
      {
        CardTypes = cardTypes.Count > 0 ? cardTypes : null,
        Subtypes = subtypes.Count > 0 ? subtypes : null,
      },
      Count = LiteralQuantity.Of(1),
      Destination = SearchDestination.Battlefield,
      Revealed = false,
    };

    var shuffleEffect = new ShuffleEffect
    {
      Player = ObjectReference.You(),
    };

    return new CompositeEffect
    {
      Effects = [searchEffect, shuffleEffect],
    };
  }
}
