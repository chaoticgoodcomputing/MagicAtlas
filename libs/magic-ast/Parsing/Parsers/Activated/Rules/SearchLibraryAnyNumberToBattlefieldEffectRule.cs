namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Search your library for any number of [filter] cards, put them onto the
/// battlefield[tapped], then shuffle."
///
/// <para>
/// The "any number" variant of the search-to-battlefield tutor pattern, distinct
/// from <see cref="SearchLibraryToBattlefieldEffectRule"/> which handles the
/// singular "search for a[n] … card, put it onto the battlefield" shape. Here
/// the oracle text uses "any number" (an open-ended count) and "them" (plural
/// pronoun) rather than "a" (singular) and "it".
/// </para>
///
/// <para>
/// The World Tree's fourth ability is the canonical example:
/// "Search your library for any number of God cards, put them onto the
/// battlefield, then shuffle."
/// </para>
///
/// <para>
/// Maps to a <see cref="CompositeEffect"/> containing:
/// <list type="number">
///   <item><see cref="SearchLibraryEffect"/> with
///     <c>Count = AnyAmountQuantity</c> and
///     <c>Destination = Battlefield</c> (or <c>BattlefieldTapped</c> when the
///     oracle says "tapped").</item>
///   <item><see cref="ShuffleEffect"/> targeting the controller (You).</item>
/// </list>
/// </para>
///
/// <para>
/// CR 701.23a: "To search for a card in a zone, look at all cards in that zone
/// (even if it's a hidden zone) and find a card that matches the given
/// description." CR 701.23b: "If a player is searching a hidden zone for cards
/// with a stated quality … that player isn't required to find some or all of
/// those cards even if they're present in that zone." — "any number" is the
/// oracle encoding of this optionality.
/// </para>
///
/// <para>
/// Priority 66 — one above <see cref="SearchLibraryToBattlefieldEffectRule"/>
/// (priority 65) so this rule's "any number of … cards, put them" pattern is
/// tried before that rule's "a[n] … card, put it" pattern. Both patterns are
/// anchored (^…$) so there is no substring-overlap risk.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 66)]
public sealed class SearchLibraryAnyNumberToBattlefieldEffectRule : IActivatedEffectRule
{
  // Matches: "Search your library for any number of <qual> cards,
  //           put them onto the battlefield[tapped][,] then shuffle."
  // Named groups:
  //   <qual>   — the qualifier phrase (e.g. "God", "basic land")
  //   <tapped> — optional "tapped" qualifier
  private static readonly Regex _pattern = new(
    @"^search\s+your\s+library\s+for\s+any\s+number\s+of\s+"
    + @"(?<qual>.+?)\s+cards,"
    + @"\s*put\s+them\s+onto\s+the\s+battlefield(?:\s+(?<tapped>tapped))?,?\s*then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG supertypes — qualifier words that belong on the Supertypes axis.
  private static readonly HashSet<string> _knownSupertypes = new(
    StringComparer.OrdinalIgnoreCase)
  {
    "basic", "legendary", "snow", "world", "ongoing",
  };

  // Known card types (singular, lowercase).
  private static readonly HashSet<string> _knownCardTypes = new(
    StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var qualRaw = m.Groups["qual"].Value.Trim();
    var isTapped = m.Groups["tapped"].Success;

    var filter = BuildFilter(qualRaw);
    if (filter is null)
    {
      return null;
    }

    var searchEffect = new SearchLibraryEffect
    {
      Filter = filter,
      Count = new AnyAmountQuantity(),
      Destination = isTapped ? SearchDestination.BattlefieldTapped : SearchDestination.Battlefield,
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

  /// <summary>
  /// Decomposes the qualifier (everything between "any number of " and " cards")
  /// into a structured <see cref="ObjectFilter"/>.
  ///
  /// Handles three shapes:
  /// <list type="bullet">
  ///   <item>Bare subtype word, e.g. "God" → Subtypes=["God"].</item>
  ///   <item>Bare card-type word, e.g. "creature" → CardTypes=["creature"].</item>
  ///   <item>Supertype + card-type, e.g. "basic land" → Supertypes=["Basic"],
  ///     CardTypes=["land"].</item>
  /// </list>
  /// </summary>
  private ObjectFilter? BuildFilter(string qual)
  {
    if (string.IsNullOrWhiteSpace(qual))
    {
      return new ObjectFilter();
    }

    var parts = qual.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0)
    {
      return new ObjectFilter();
    }

    // Single word.
    if (parts.Length == 1)
    {
      var word = parts[0];
      if (_knownCardTypes.Contains(word))
      {
        return new ObjectFilter { CardTypes = [word.ToLowerInvariant()] };
      }
      if (_knownSupertypes.Contains(word))
      {
        return new ObjectFilter { Supertypes = [Titled(word)] };
      }
      // Treat as creature subtype / tribal name (e.g. "God").
      return new ObjectFilter { Subtypes = [Titled(word)] };
    }

    // Multi-word: partition into supertypes + card type or subtypes.
    var last = parts[^1];
    if (_knownCardTypes.Contains(last) && parts.Length > 1)
    {
      var supertypes = new List<string>();
      var subtypes = new List<string>();
      foreach (var w in parts[..^1])
      {
        (_knownSupertypes.Contains(w) ? supertypes : subtypes).Add(Titled(w));
      }
      return new ObjectFilter
      {
        CardTypes = [last.ToLowerInvariant()],
        Supertypes = supertypes.Count > 0 ? supertypes : null,
        Subtypes = subtypes.Count > 0 ? subtypes : null,
      };
    }

    // Whole qualifier is a subtype phrase.
    var subtype = string.Join(' ', parts.Select(Titled));
    return new ObjectFilter { Subtypes = [subtype] };
  }

  private static string Titled(string word) =>
    word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
}
