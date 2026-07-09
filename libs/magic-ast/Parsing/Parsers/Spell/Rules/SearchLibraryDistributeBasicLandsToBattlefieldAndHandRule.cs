namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Search your library for up to two basic land cards, reveal those cards, put
/// one onto the battlefield tapped and the other into your hand, then shuffle."
///
/// The Cultivate / Kodama's Reach ramp family: a single search that finds up to
/// two basic lands, reveals them, and <b>distributes</b> the found cards across
/// two zones — one onto the battlefield tapped, the other into hand — before
/// shuffling. Distinct from <see cref="SearchLibraryToBattlefieldRule"/> (all
/// found cards to one zone): the split destination is the defining shape, carried
/// by <see cref="SearchLibraryEffect.Placements"/> with
/// <see cref="SearchDestination.Distributed"/> as the effect-level sentinel.
///
/// The reveal ("reveal those cards" → <c>Revealed = true</c>) and the trailing
/// "then shuffle" are folded into the single effect, matching the convention of
/// the sibling search rules. CR 701.23 (Search); CR 701.20 (Reveal).
///
/// Fully anchored (<c>^…$</c>) so there is no substring collision with the other
/// search rules.
/// </summary>
[SpellRule]
public sealed class SearchLibraryDistributeBasicLandsToBattlefieldAndHandRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Search\s+your\s+library\s+for\s+up\s+to\s+(?<count>[a-z]+)\s+basic\s+land\s+cards,\s*"
    + @"reveal\s+those\s+cards,\s*"
    + @"put\s+one\s+onto\s+the\s+battlefield\s+tapped\s+and\s+the\s+other\s+into\s+your\s+hand,\s*"
    + @"then\s+shuffle$",
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

    // "up to two" — the maximum found; "the other" fixes the split at exactly two shares.
    if (!SpellRuleHelpers.TryParseSmallWord(m.Groups["count"].Value, out var max))
    {
      return false;
    }

    effect = new SearchLibraryEffect
    {
      Filter = _basicLandFilter,
      Count = new UpToQuantity { Maximum = max, Minimum = 0 },
      Destination = SearchDestination.Distributed,
      Revealed = true,
      Placements =
      [
        new SearchPlacement
        {
          Count = LiteralQuantity.Of(1),
          Destination = SearchDestination.BattlefieldTapped,
        },
        new SearchPlacement
        {
          Count = LiteralQuantity.Of(1),
          Destination = SearchDestination.Hand,
        },
      ],
    };
    return true;
  }
}
