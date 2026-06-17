namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile a creature you control" — exile-a-permanent-you-control as an activation
/// cost. Used by Food Chain (Exodus, <c>EXO:193</c>): CR 118.8a classifies exile as
/// a cost action when it precedes the colon separator of an activated ability.
///
/// <para>The encoded cost uses <see cref="ExileCost"/> with:
///   <list type="bullet">
///     <item><c>Filter.CardTypes = ["creature"]</c></item>
///     <item><c>Filter.Controller = You</c> — "you control" restricts to the
///       activating player's own permanents (CR 109.4).</item>
///     <item><c>FromZone = Battlefield</c> — the creature is on the battlefield
///       when the ability is activated (CR 701.13a).</item>
///     <item><c>Quantity = 1</c> — "a creature" is a single object.</item>
///   </list>
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 997)]
public sealed class ExileCreatureCostRule : IActivatedCostRule
{
  // Matches "Exile a/an <type> you control" where type is a permanent card type.
  private static readonly Regex _pattern = new(
    @"^Exile\s+(?:a|an)\s+(?<type>creature|artifact|enchantment|land|permanent)\s+you\s+control$",
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
        Controller = ControllerFilter.You,
      },
      Quantity = LiteralQuantity.Of(1),
      FromZone = Zone.Battlefield,
    };
  }
}
