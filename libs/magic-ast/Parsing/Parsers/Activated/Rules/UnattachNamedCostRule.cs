namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;

/// <summary>
/// "Unattach [card name]" activation cost — the Equipment detaches itself from the
/// equipped creature as part of paying this cost. Paradigm card: KHM Toralf's Hammer,
/// whose inner activated ability includes "Unattach Toralf's Hammer" in its cost.
///
/// <para>
/// Distinct from the keyword-action <see cref="MagicAST.AST.Effects.Modification.UnattachEffect"/>
/// (an effect that unattaches) and from the parameterless reconfigure unattach ability
/// (CR 702.151a). This cost form names the Equipment explicitly, allowing the engine to
/// locate the specific permanent to unattach without ambiguity (CR 201.4: a card's name
/// in its own text is a self-reference).
/// </para>
///
/// <para>
/// The card name is captured verbatim from oracle text (e.g. "Toralf's Hammer") and
/// stored in <see cref="UnattachNamedCost.CardName"/> for reference-not-resolution
/// (ADR 0004 — MAST records the reference; the engine resolves the identity).
/// Anchored (^…$) to prevent matching a standalone word in a multi-component cost.
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 997)]
public sealed class UnattachNamedCostRule : IActivatedCostRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Unattach\s+(?<name>.+)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText)
  {
    var m = _pattern.Match(costText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var cardName = m.Groups["name"].Value.Trim();
    if (cardName.Length == 0)
    {
      return null;
    }

    return new UnattachNamedCost { CardName = cardName };
  }
}
