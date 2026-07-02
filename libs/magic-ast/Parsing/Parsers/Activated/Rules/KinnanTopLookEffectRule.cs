namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the Kinnan, Bonder Prodigy activated ability effect:
/// "Look at the top five cards of your library. You may put a non-Human creature card
///  from among them onto the battlefield. Put the rest on the bottom of your library
///  in a random order."
///
/// <para>
/// The three-sentence fragment is one atomic action — the "from among them" and "the rest"
/// back-reference the same looked-at pile — and is emitted as a single
/// <see cref="TopLookPutOntoBattlefieldEffect"/>. Splitting sentences would break the
/// pile-binding. CR 701.12 (look); CR 400.7 (onto the battlefield); CR 400.4 (random order).
/// </para>
///
/// <para>
/// The non-Human creature filter is encoded as
/// <c>CardTypes=["creature"], ExcludedSubtypes=["Human"]</c> per
/// <see cref="MagicAST.AST.References.ObjectFilter.ExcludedSubtypes"/> semantics (parallel to
/// the Mutate "target non-Human creature" shape). CR 205.3m (creature subtypes).
/// </para>
///
/// <para>Priority 95: must beat generic look-at-top sentence dispatch.</para>
/// </summary>
[ActivatedEffectRule(Priority = 95)]
public sealed class KinnanTopLookEffectRule : IActivatedEffectRule
{
  // Matches the full three-sentence effect text (period-trimmed):
  //   "Look at the top N cards of your library.
  //    You may put a [filter] card from among them onto the battlefield.
  //    Put the rest on the bottom of your library in a random order"
  // The <count> group captures a digit or number word.
  // The <filter> group captures the card qualifier before "card from among them".
  private static readonly Regex _pattern = new(
    @"^Look\s+at\s+the\s+top\s+(?<count>\d+|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+of\s+your\s+library\.\s+"
    + @"You\s+may\s+put\s+a\s+(?<filter>.+?)\s+card\s+from\s+among\s+them\s+onto\s+the\s+battlefield\.\s+"
    + @"Put\s+the\s+rest\s+on\s+the\s+bottom\s+of\s+your\s+library\s+in\s+a\s+random\s+order$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var countRaw = match.Groups["count"].Value.ToLowerInvariant();
    var count = ParseCount(countRaw);
    if (count is null)
    {
      return null;
    }

    var filterRaw = match.Groups["filter"].Value.Trim();
    var cardFilter = BuildFilter(filterRaw);
    if (cardFilter is null)
    {
      return null;
    }

    return new TopLookPutOntoBattlefieldEffect
    {
      Count = LiteralQuantity.Of(count.Value),
      Player = ObjectReference.You(),
      CardFilter = cardFilter,
      Optional = true,
    };
  }

  /// <summary>
  /// Parses the card qualifier (e.g. "non-Human creature") into an
  /// <see cref="ObjectFilter"/>. Handles the "non-[Subtype] [card-type]" pattern.
  /// </summary>
  private static ObjectFilter? BuildFilter(string qualifier)
  {
    // "non-Human creature" → CardTypes=["creature"], ExcludedSubtypes=["Human"]
    // Uses the parallel ExcludedSubtypes axis (same as Mutate targeting).
    var nonSubtypeMatch = Regex.Match(
      qualifier,
      @"^non-(?<excl>[A-Z][a-z]+)\s+(?<type>creature|artifact|enchantment|planeswalker|land|permanent)$",
      RegexOptions.IgnoreCase
    );
    if (nonSubtypeMatch.Success)
    {
      var excludedSubtype = nonSubtypeMatch.Groups["excl"].Value;
      var cardType = nonSubtypeMatch.Groups["type"].Value.ToLowerInvariant();
      return new ObjectFilter
      {
        CardTypes = [cardType],
        ExcludedSubtypes = [excludedSubtype],
      };
    }

    // Fallback: plain card-type without exclusion (e.g. "creature")
    var plainTypeMatch = Regex.Match(
      qualifier,
      @"^(creature|artifact|enchantment|planeswalker|land|permanent)$",
      RegexOptions.IgnoreCase
    );
    if (plainTypeMatch.Success)
    {
      return new ObjectFilter
      {
        CardTypes = [plainTypeMatch.Value.ToLowerInvariant()],
      };
    }

    return null;
  }

  private static int? ParseCount(string raw) =>
    raw switch
    {
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ when int.TryParse(raw, out var n) => n,
      _ => null,
    };
}
