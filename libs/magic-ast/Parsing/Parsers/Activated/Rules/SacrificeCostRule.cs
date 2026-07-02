namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Sacrifice cost: "Sacrifice another creature", "Sacrifice X Squirrels", etc.
/// Reuses the shared <see cref="ActivatedRuleHelpers.ParseSacrificePattern"/> for most
/// patterns; handles "nonland permanent(s)" directly before delegating.
/// </summary>
[ActivatedCostRule(Priority = 999)]
public sealed class SacrificeCostRule : IActivatedCostRule
{
  // Matches "Sacrifice N nonland permanents" — the "nonland permanents" (plural)
  // pattern does not route through ParseSacrificePattern correctly because the shared
  // helper's \bpermanent\b word-boundary regex does not match "permanents" (the 's'
  // breaks the word boundary). Handle it directly here, then delegate the rest.
  private static readonly Regex _nonlandPermanentPattern = new(
    @"^Sacrifice\s+(?<count>one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+nonland\s+permanents?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    if (!lower.StartsWith("sacrifice"))
    {
      return null;
    }

    // Early intercept: "Sacrifice N nonland permanents" (Bolas's Citadel, WAR).
    // CR 205.3a: land is a card type; "nonland" excludes it via ExcludedCardTypes.
    // Must run BEFORE ParseSacrificePattern because that helper uses \bpermanent\b
    // which does not match the plural "permanents", causing it to misparse "ten" as
    // a subtype name.
    var nonlandMatch = _nonlandPermanentPattern.Match(costText);
    if (nonlandMatch.Success)
    {
      var rawCount = nonlandMatch.Groups["count"].Value.ToLowerInvariant();
      int count = rawCount switch
      {
        "one" => 1, "two" => 2, "three" => 3, "four" => 4, "five" => 5,
        "six" => 6, "seven" => 7, "eight" => 8, "nine" => 9, "ten" => 10,
        _ => int.Parse(rawCount),
      };
      return new SacrificeCost
      {
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          ExcludedCardTypes = ["land"],
        },
        Quantity = LiteralQuantity.Of(count),
      };
    }

    var (quantity, filter) = ActivatedRuleHelpers.ParseSacrificePattern(costText);
    if (filter == null)
    {
      return null;
    }

    // "Sacrifice a creature of the chosen type" — the sacrificed permanent must
    // match the creature type chosen as this permanent entered. This is the
    // consumer half of a CR 607 linked ability (the producer is the
    // "choose a creature type" effect under StaticAbility.When = AsThisEnters):
    //   CR 607.1: "An object may have two abilities printed on it such that one of
    //   them causes actions to be taken or objects or players to be affected and
    //   the other one directly refers to those actions, objects, or players. If so,
    //   these two abilities are linked: the second refers only to actions that were
    //   taken or objects or players that were affected by the first, and not by any
    //   other ability."
    // Encode it as the structured ObjectFilter.ChosenCharacteristic reference rather
    // than dropping the phrase (the bare helper does the latter). Sacrificing here
    // moves the chosen-type creature to the graveyard as the activation cost
    // (CR 701.21a).
    if (lower.Contains("of the chosen type"))
    {
      filter = filter with { ChosenCharacteristic = ChosenCharacteristicKind.CreatureType };
    }

    return new SacrificeCost { Filter = filter, Quantity = quantity };
  }
}
