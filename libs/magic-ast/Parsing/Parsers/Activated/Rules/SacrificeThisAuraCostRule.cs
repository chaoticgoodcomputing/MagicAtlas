namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Sacrifice-self cost naming the Aura subtype specifically: "Sacrifice this Aura"
/// (Floating Shield). Aura is an enchantment SUBTYPE (CR 205.3h), not a base card
/// type, so the shared <see cref="ActivatedRuleHelpers.ParseSacrificePattern"/> —
/// whose "this &lt;type&gt;" branches only recognise the base card-type nouns
/// (creature/permanent/artifact/enchantment/land) — falls through to its generic
/// word-capture fallback, which captures only the bare word "this" as a garbage
/// <c>Subtypes</c> entry and silently drops "Aura". Intercepting here with a NEW,
/// narrowly-anchored rule (priority above <see cref="SacrificeCostRule"/>'s 999)
/// closes that gap without touching the shared helper (which many other cards'
/// "Sacrifice [creature/permanent/artifact/enchantment/land]" costs route through).
///
/// CR 701.21a: sacrificing is a cost paid by moving the permanent from the
/// battlefield to its owner's graveyard. Encoded as
/// <c>CardTypes=["enchantment"]</c> + <c>Subtypes=["Aura"]</c> + <c>IsSelf=true</c> —
/// the structured self-reference (parallel to the "this enchantment"/"this
/// creature" branches of <see cref="ActivatedRuleHelpers.ParseSacrificePattern"/>),
/// narrowed with the Aura subtype since the oracle text names the subtype
/// specifically rather than the bare card type.
/// </summary>
[ActivatedCostRule(Priority = 1002)]
public sealed class SacrificeThisAuraCostRule : IActivatedCostRule
{
  // Anchored: matches the whole cost component so it cannot fire as a substring of
  // a longer, differently-shaped cost fragment.
  private static readonly Regex _pattern = new(
    @"^Sacrifice\s+this\s+Aura$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText)
  {
    var trimmed = costText.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new SacrificeCost
    {
      Filter = new ObjectFilter
      {
        CardTypes = ["enchantment"],
        Subtypes = ["Aura"],
        IsSelf = true,
      },
      Quantity = LiteralQuantity.Of(1),
    };
  }
}
