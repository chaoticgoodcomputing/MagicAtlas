namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Tap-permanents cost: "Tap two untapped artifacts you control",
/// "Tap an untapped creature you control", etc. — the spelled-out "Tap …" verb
/// (not the {T} symbol) used as an activation cost (CR 118.3 / 602.5; CR 701.20
/// tap/untap). e.g. Whirler Rogue: "Tap two untapped artifacts you control:
/// Target creature can't be blocked this turn."
///
/// Emits a <see cref="TapPermanentsCost"/> whose <see cref="TapPermanentsCost.Filter"/>
/// carries the tapped permanents' card type and controller. The "untapped"
/// qualifier is implicit in the cost (a permanent must be untapped to be tapped
/// to pay the cost — CR 701.20a), so it is not encoded as a separate axis;
/// this matches the existing Conspire <see cref="TapPermanentsCost"/> idiom
/// ("tap two untapped creatures you control").
/// </summary>
[ActivatedCostRule(Priority = 997)]
public sealed class TapPermanentsCostRule : IActivatedCostRule
{
  public Cost? TryMatch(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    // Only the spelled-out "Tap …" verb. The {T}/{Q} symbol forms are handled
    // by the mana/tap symbol cost path (IsCostToken).
    if (!lower.StartsWith("tap "))
    {
      return null;
    }

    // Card type of the permanents to tap (CR 205.3a card types).
    string? cardType = null;
    if (Regex.IsMatch(lower, @"\bartifacts?\b"))
    {
      cardType = "artifact";
    }
    else if (Regex.IsMatch(lower, @"\bcreatures?\b"))
    {
      cardType = "creature";
    }
    else if (Regex.IsMatch(lower, @"\benchantments?\b"))
    {
      cardType = "enchantment";
    }
    else if (Regex.IsMatch(lower, @"\blands?\b"))
    {
      cardType = "land";
    }
    else if (Regex.IsMatch(lower, @"\bpermanents?\b"))
    {
      cardType = "permanent";
    }

    if (cardType is null)
    {
      return null;
    }

    var controller = lower.Contains("you control") ? ControllerFilter.You : (ControllerFilter?)null;

    var count = ActivatedRuleHelpers.ParseNumberWord(costText) ?? 1;

    return new TapPermanentsCost
    {
      Filter = new ObjectFilter { CardTypes = [cardType], Controller = controller },
      Quantity = LiteralQuantity.Of(count),
    };
  }
}
