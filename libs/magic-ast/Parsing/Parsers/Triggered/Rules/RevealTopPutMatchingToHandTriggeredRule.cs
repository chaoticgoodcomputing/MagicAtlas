namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Reveal the top N cards of your library. Put all [filter] cards revealed this
/// way into your hand and the rest on the bottom of your library in any order." —
/// the Sylvan Messenger ETB reveal-and-partition family.
///
/// <para>
/// The whole two-sentence fragment is one coherent game action — "revealed this
/// way" and "the rest" in the second sentence are back-references to the reveal
/// in the first — so this rule matches both sentences as a single fragment and
/// emits one <see cref="RevealTopPutMatchingToHandEffect"/>. CR 701.20 (Reveal);
/// CR 401.4 (the remainder placed on the bottom in any order, player-chosen).
/// </para>
///
/// <para>
/// Priority 95: must beat the generic multi-sentence bundle splitter, which would
/// otherwise try to resolve each sentence independently and fail (mirroring
/// <see cref="ThassasOracleRule"/>).
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class RevealTopPutMatchingToHandTriggeredRule : ITriggeredRule
{
  // Matches: "reveal the top <count> cards of your library. Put all <filter> cards
  // revealed this way into your hand and the rest on the bottom of your library in
  // any order". <count> may be a digit or a word number; <filter> is a single
  // qualifying word (a subtype, e.g. "Elf").
  private static readonly Regex _pattern = new(
    @"^reveal\s+the\s+top\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+of\s+your\s+library\.\s*"
    + @"Put\s+all\s+(?<filter>[a-z]+)\s+cards\s+revealed\s+this\s+way\s+into\s+your\s+hand\s+and\s+the\s+rest\s+on\s+the\s+bottom\s+of\s+your\s+library\s+in\s+any\s+order$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG card types (singular, lowercase). A filter word matching one of
  // these is a card type, not a subtype (mirrors SearchLibraryToHandTriggeredRule).
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var count = ParseCount(match.Groups["count"].Value.ToLowerInvariant());
    if (count is null)
    {
      return false;
    }

    var filterWord = match.Groups["filter"].Value;
    ObjectFilter filter;
    if (_knownCardTypes.Contains(filterWord))
    {
      filter = new ObjectFilter
      {
        CardTypes = [filterWord.ToLowerInvariant()],
      };
    }
    else
    {
      // Title-case the word to match oracle convention (e.g. "Elf", "Goblin").
      var subtype = char.ToUpperInvariant(filterWord[0]) + filterWord[1..].ToLowerInvariant();
      filter = new ObjectFilter
      {
        Subtypes = [subtype],
      };
    }

    effect = new RevealTopPutMatchingToHandEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(count.Value),
      Filter = filter,
    };
    return true;
  }

  private static int? ParseCount(string raw) =>
    raw switch
    {
      "one" => 1,
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
