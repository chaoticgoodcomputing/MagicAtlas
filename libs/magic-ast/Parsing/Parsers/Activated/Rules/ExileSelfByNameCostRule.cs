namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile [CardName]" — exile the source permanent by its own card name as an
/// activation cost. Rule 201.5: if an object's oracle text refers to the object
/// it is applying to by name, that name refers to that object.
///
/// <para>
/// Encodes the cost as <see cref="ExileCost"/> with:
///   <list type="bullet">
///     <item><c>Filter.IsSelf = true</c> — the oracle name refers to the source
///       permanent itself (CR 201.5 self-reference by card name).</item>
///     <item><c>Filter.CardTypes = ["permanent"]</c> — we don't have the type
///       line here; "permanent" is the conservative common type, which the
///       SelfReferenceTypeCorrector in CardParser will narrow at the card level.</item>
///     <item><c>FromZone = Battlefield</c> — the permanent is on the battlefield
///       when the ability is activated (CR 701.13a moves it from there to exile).</item>
///     <item><c>Quantity = 1</c> — a single object.</item>
///   </list>
/// </para>
///
/// <para>
/// Distinct from <see cref="ExileSelfCostRule"/> which matches the "Exile this [type]"
/// form. This rule matches the self-by-name form used when a card names itself
/// explicitly (Gonti's Aether Heart: "Exile Gonti's Aether Heart").
/// </para>
///
/// <para>
/// ANCHOR: pattern is anchored (^...$) to prevent matching substrings inside a
/// longer exile-as-effect clause (e.g. "Exile target creature." in an effect body).
/// Only fires when the entire cost component is "Exile [Name]".
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 996)]
public sealed class ExileSelfByNameCostRule : IActivatedCostRule
{
  // Matches "Exile <CamelCase or apostrophe words>". The name begins with a capital
  // letter (CR 201.4 card names always start with a capital). Must NOT match
  // "Exile this [type]" (handled by ExileSelfCostRule at Priority 998),
  // "Exile a/an [type]" (ExileCreatureCostRule at Priority 997),
  // or "Exile target [filter]" (those carry "target" immediately after "Exile").
  // Apostrophes and hyphens are allowed inside name tokens (Gonti's, Toralf's).
  private static readonly Regex _pattern = new(
    @"^Exile\s+(?<name>[A-Z][A-Za-z''\-]*(?:\s+[A-Za-z''\-]+)*)$",
    RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText)
  {
    var trimmed = costText.Trim();

    // Guard: don't claim "Exile this [type]", "Exile a/an [type]", "Exile target [filter]"
    var lower = trimmed.ToLowerInvariant();
    if (lower.StartsWith("exile this ")
        || lower.StartsWith("exile a ")
        || lower.StartsWith("exile an ")
        || lower.StartsWith("exile target "))
    {
      return null;
    }

    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    return new ExileCost
    {
      Filter = new ObjectFilter
      {
        CardTypes = ["permanent"],
        IsSelf = true,
      },
      Quantity = LiteralQuantity.Of(1),
      FromZone = Zone.Battlefield,
    };
  }
}
