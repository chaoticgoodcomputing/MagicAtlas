namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Search your library for (a basic land card|up to N basic land cards),
///  put (it|them) onto the battlefield tapped, then shuffle."
///
/// Covers the sorcery ramp pattern (e.g. Rampant Growth, Beneath the Sands).
/// Always non-optional (no "you may" prefix). Destination is always BattlefieldTapped.
/// </summary>
[SpellRule]
public sealed class SearchLibraryToBattlefieldRule : ISpellRule
{
  // Matches singular ("a basic land card") and plural ("up to N basic land cards") forms.
  private static readonly Regex _pattern = new(
    @"^Search\s+your\s+library\s+for\s+(?:(?<upto>up\s+to\s+)?(?<count>[a-z]+|\d+)\s+)?basic\s+land\s+cards?,"
    + @"\s*put\s+(?:it|them)\s+onto\s+the\s+battlefield\s+tapped,\s*then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly ObjectFilter _basicLandFilter = new()
  {
    Supertypes = ["Basic"],
    CardTypes = ["land"],
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    // Determine count. When the count group is missing the phrase was just
    // "a basic land card" (singular form with no explicit count word before it);
    // default to 1.
    Quantity count;
    var countToken = m.Groups["count"].Success ? m.Groups["count"].Value : "a";
    var isUpTo = m.Groups["upto"].Success;
    var n = SpellRuleHelpers.ParseSmallWord(countToken);

    count = isUpTo ? (Quantity)new UpToQuantity { Maximum = n, Minimum = 0 } : LiteralQuantity.Of(n);

    effect = new SearchLibraryEffect
    {
      Filter = _basicLandFilter,
      Count = count,
      Destination = SearchDestination.BattlefieldTapped,
      Revealed = false,
    };
    return true;
  }
}
