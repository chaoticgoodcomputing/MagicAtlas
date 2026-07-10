namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Search your library for (a|up to N) [basic land type] card(s), put
///  (it|them|that card) onto the battlefield, then shuffle."
///
/// The land-fetch ramp sorcery keyed to a specific basic land TYPE (Plains,
/// Island, Swamp, Mountain, or Forest) that puts the found land onto the
/// battlefield UNTAPPED — e.g. Nature's Lore, Three Visits ("a Forest card"),
/// Skyshroud Claim ("up to two Forest cards"). A "Forest card" is any card with
/// the Forest land subtype (CR 305.6: "The basic land types are Plains, Island,
/// Swamp, Mountain, and Forest."), so the resulting filter carries
/// <c>CardTypes=["land"]</c> plus the named <c>Subtype</c>. No "basic" supertype is
/// asserted — the card names only the subtype, so non-basic Forest cards (dual
/// lands, snow lands) qualify too (mirrors the tapped sibling's encoding).
///
/// The search action is CR 701.23. Untapped twin of
/// <see cref="SearchLibraryLandTypeToBattlefieldRule"/>: that one anchors on
/// "...battlefield tapped, then shuffle"; this one anchors on the bare
/// "...battlefield, then shuffle" (no "tapped" token), so the two regexes are
/// mutually exclusive and destination is always <see cref="SearchDestination.Battlefield"/>.
/// Always non-optional (no "you may" prefix).
/// </summary>
[SpellRule]
public sealed class SearchLibraryLandTypeToBattlefieldUntappedRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Search\s+your\s+library\s+for\s+(?:(?<upto>up\s+to\s+)?(?<count>[a-z]+|\d+)\s+)?"
    + @"(?<type>Plains|Island|Swamp|Mountain|Forest)\s+cards?,"
    + @"\s*put\s+(?:it|them|that\s+card)\s+onto\s+the\s+battlefield,\s*then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    // Count. When absent the phrase was "a [type] card" (singular, no explicit
    // count word); default to 1, matching the tapped sibling.
    var countToken = m.Groups["count"].Success ? m.Groups["count"].Value : "a";
    var isUpTo = m.Groups["upto"].Success;
    var n = SpellRuleHelpers.ParseSmallWord(countToken);
    Quantity count = isUpTo
      ? new UpToQuantity { Maximum = n, Minimum = 0 }
      : LiteralQuantity.Of(n);

    // Canonicalize the captured land type (IgnoreCase could capture any casing).
    var raw = m.Groups["type"].Value;
    var subtype = char.ToUpperInvariant(raw[0]) + raw[1..].ToLowerInvariant();

    effect = new SearchLibraryEffect
    {
      Filter = new ObjectFilter
      {
        CardTypes = ["land"],
        Subtypes = [subtype],
      },
      Count = count,
      Destination = SearchDestination.Battlefield,
      Revealed = false,
    };
    return true;
  }
}
