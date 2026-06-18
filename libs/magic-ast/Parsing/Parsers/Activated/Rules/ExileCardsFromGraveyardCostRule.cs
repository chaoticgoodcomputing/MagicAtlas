namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile N [type] cards from your graveyard" — exile-from-graveyard as an activation
/// cost. Used by Drivnod, Carnage Dominus: "Exile three creature cards from your
/// graveyard". CR 118.8a classifies exile as a cost action when it precedes the colon
/// separator of an activated ability.
///
/// <para>The encoded cost uses <see cref="ExileCost"/> with:
///   <list type="bullet">
///     <item><c>Filter.CardTypes</c> — the specified card type (e.g. "creature").</item>
///     <item><c>Filter.Controller = You</c> — "your graveyard" restricts to the
///       activating player's own graveyard (CR 109.4).</item>
///     <item><c>FromZone = Graveyard</c> — the cards are in the graveyard when
///       the ability is activated (CR 406).</item>
///     <item><c>Quantity</c> — the stated count (e.g. three → 3).</item>
///   </list>
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 996)]
public sealed class ExileCardsFromGraveyardCostRule : IActivatedCostRule
{
  // Matches: "Exile N <type> cards from your graveyard"
  // where N is a number word (one/two/three/…) or digit, and <type> is a card type.
  private static readonly Regex _pattern = new(
    @"^Exile\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>creature|artifact|enchantment|land|permanent|card)\s+cards?\s+from\s+your\s+graveyard$",
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

    var typeRaw = match.Groups["type"].Value.ToLowerInvariant();

    return new ExileCost
    {
      Filter = new ObjectFilter
      {
        CardTypes = [typeRaw],
        Controller = ControllerFilter.You,
      },
      Quantity = LiteralQuantity.Of(quantity),
      FromZone = Zone.Graveyard,
    };
  }
}
