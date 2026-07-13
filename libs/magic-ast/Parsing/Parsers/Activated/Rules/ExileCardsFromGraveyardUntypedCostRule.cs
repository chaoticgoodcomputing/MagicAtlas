namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile N cards from your graveyard" — exile-from-graveyard as an activation cost,
/// with no card-type qualifier (any card, not "creature card"/"artifact card"/etc.).
/// Used by Psychatog: "Exile two cards from your graveyard: This creature gets +1/+1
/// until end of turn." CR 118.8a classifies exile as a cost action when it precedes
/// the colon separator of an activated ability.
///
/// <para>Sibling of <see cref="ExileCardsFromGraveyardCostRule"/>, which requires a
/// card-type word between the count and "cards" (e.g. "three creature cards"). This
/// rule is anchored (^…$) to only the untyped "N cards" phrasing, so the two never
/// collide: this rule's pattern has no type-word capture group and therefore cannot
/// match "Exile three creature cards from your graveyard", and the typed rule's
/// pattern requires a type word so it cannot match "Exile two cards from your
/// graveyard".</para>
///
/// <para>The encoded cost uses <see cref="ExileCost"/> with:
///   <list type="bullet">
///     <item><c>Filter.CardTypes = ["card"]</c> — no type restriction beyond "card"
///       (mirrors the untyped <see cref="DiscardCost"/> convention for "Discard a
///       card").</item>
///     <item><c>Filter.Controller = You</c> — "your graveyard" restricts to the
///       activating player's own graveyard (CR 109.4).</item>
///     <item><c>FromZone = Graveyard</c> — the cards are in the graveyard when
///       the ability is activated (CR 406).</item>
///     <item><c>Quantity</c> — the stated count (e.g. two → 2).</item>
///   </list>
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 996)]
public sealed class ExileCardsFromGraveyardUntypedCostRule : IActivatedCostRule
{
  // Matches: "Exile N cards from your graveyard" (no type word between count and "cards").
  private static readonly Regex _pattern = new(
    @"^Exile\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?\s+from\s+your\s+graveyard$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, int> _wordToInt =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["a"] = 1,
      ["an"] = 1,
      ["one"] = 1,
      ["two"] = 2,
      ["three"] = 3,
      ["four"] = 4,
      ["five"] = 5,
      ["six"] = 6,
      ["seven"] = 7,
      ["eight"] = 8,
      ["nine"] = 9,
      ["ten"] = 10,
    };

  public Cost? TryMatch(string costText)
  {
    var trimmed = costText.Trim();
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var countRaw = match.Groups["count"].Value;
    int quantity;
    if (!_wordToInt.TryGetValue(countRaw, out quantity))
    {
      if (!int.TryParse(countRaw, out quantity))
      {
        return null;
      }
    }

    return new ExileCost
    {
      Filter = new ObjectFilter
      {
        CardTypes = ["card"],
        Controller = ControllerFilter.You,
      },
      Quantity = LiteralQuantity.Of(quantity),
      FromZone = Zone.Graveyard,
    };
  }
}
