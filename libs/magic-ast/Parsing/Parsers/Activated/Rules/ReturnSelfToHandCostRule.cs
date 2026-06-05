namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.References;

/// <summary>
/// Return-self-to-hand cost: "Return this creature to its owner's hand" (Grinning Ignus), "Return this
/// enchantment to its owner's hand" (Recurring Nightmare), etc. — the activated permanent is moved to
/// the Hand zone as part of paying the cost (CR 701.x), so it must be recast to use the ability again.
///
/// Previously this clause, when in the <em>cost</em> position (before the colon), matched no cost rule
/// and was silently dropped — leaving the ability's <c>Costs</c> incomplete (or empty). The companion
/// <see cref="ReturnSelfToHandEffectRule"/> handles the same phrase in the <em>effect</em> position; the
/// two run in different parsing phases, so they don't conflict. Mirrors <see cref="ExileSelfCostRule"/>.
/// </summary>
[ActivatedCostRule(Priority = 998)]
public sealed class ReturnSelfToHandCostRule : IActivatedCostRule
{
  private static readonly Regex _pattern = new(
    @"^Return\s+this\s+(?:creature|permanent|aura|enchantment|artifact|land)\s+to\s+its\s+owner's\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText) =>
    _pattern.IsMatch(costText.Trim())
      ? new ReturnToHandCost { Target = ObjectReference.Self() }
      : null;
}
