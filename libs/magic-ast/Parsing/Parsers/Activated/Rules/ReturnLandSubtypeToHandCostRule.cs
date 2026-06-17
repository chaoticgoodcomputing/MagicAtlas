namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.References;

/// <summary>
/// Return-a-land-subtype-you-control-to-hand cost:
/// "Return a Forest you control to its owner's hand" — Quirion Ranger (CR 602).
///
/// Distinct from <see cref="ReturnSelfToHandCostRule"/> (self-bounce) and the effect-position
/// <see cref="ReturnSelfToHandEffectRule"/>: this is a return of a controller-chosen permanent
/// (a Forest the controller owns) as the activation cost. The returned permanent is identified
/// by its land subtype (Forest, Island, Swamp, Mountain, Plains) and the controller constraint.
///
/// CR 118.3: any game action may be a cost; CR 602: activated abilities have a cost before the
/// colon. The cost here moves the chosen land to its owner's hand as part of paying.
/// ObjectReferenceKind.Any (not Target): the controller selects any qualifying permanent at
/// activation time — not a legal target (Rule 115.1 — "target" requires the word "target").
/// </summary>
[ActivatedCostRule(Priority = 997)]
public sealed class ReturnLandSubtypeToHandCostRule : IActivatedCostRule
{
  // Anchored pattern: "Return a <subtype> you control to its owner's hand"
  // Accepts any single-word land subtype (Forest, Island, Swamp, Mountain, Plains, or
  // any future basic land type). The leading anchor avoids substring collisions with
  // self-bounce phrasing ("Return this creature to its owner's hand").
  private static readonly Regex _pattern = new(
    @"^Return\s+a\s+(?<subtype>[A-Za-z]+)\s+you\s+control\s+to\s+its\s+owner's\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText)
  {
    var m = _pattern.Match(costText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var subtype = m.Groups["subtype"].Value;

    // Any — controller picks one qualifying permanent at resolution; no "target" keyword
    // so this is NOT a targeted ability (CR 115.1). ObjectReferenceKind.Any is the
    // correct reference kind for "a [filter] you control" in a cost position.
    return new ReturnToHandCost
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter
        {
          CardTypes = ["land"],
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
        },
      },
    };
  }
}
