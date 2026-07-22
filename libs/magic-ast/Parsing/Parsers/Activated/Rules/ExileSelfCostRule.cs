namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Exile-self cost: "Exile this artifact", "Exile this creature", etc.
/// The activated permanent is moved to the exile zone as part of paying
/// the activation cost.
///
/// CR 701.13a: "To exile an object, move it to the exile zone from wherever it is."
///
/// The encoded cost uses <see cref="ExileCost"/> with:
///   - <c>Filter.CardTypes</c> = the permanent type named in oracle text
///   - <c>Filter.Characteristics</c> = <c>[OtherCharacteristic("this permanent")]</c>
///     to record the self-referential "this" phrasing (parallel to the
///     <c>SacrificeCostRule</c> convention for "Sacrifice this artifact" costs)
///   - <c>FromZone</c> = <c>Battlefield</c> (the permanent is on the battlefield
///     when the ability is activated; CR 701.13a moves it from there to exile)
///   - <c>Quantity</c> = 1 (oracle text always names a single permanent)
/// </summary>
[ActivatedCostRule(Priority = 998)]
public sealed class ExileSelfCostRule : IActivatedCostRule
{
  // Matches "Exile this <type>" where <type> is a permanent type word.
  private static readonly Regex _pattern = new(
    @"^Exile\s+this\s+(?<type>artifact|creature|enchantment|land|permanent|planeswalker)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText)
  {
    var trimmed = costText.Trim();
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var typeRaw = match.Groups["type"].Value.ToLowerInvariant();

    return new ExileCost
    {
      Filter = new ObjectFilter
      {
        CardTypes = [typeRaw],
        // "this permanent"/"this artifact" is the source object itself (CR 109) — the
        // first-class IsSelf axis, not a free-text residual.
        IsSelf = true,
      },
      Quantity = LiteralQuantity.Of(1),
      FromZone = Zone.Battlefield,
    };
  }
}
