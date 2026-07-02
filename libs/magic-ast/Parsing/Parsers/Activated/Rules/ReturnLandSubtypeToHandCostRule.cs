namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.References;

/// <summary>
/// Return-a-land-you-control-to-hand cost:
/// "Return a Forest you control to its owner's hand" — Quirion Ranger (CR 602);
/// "Return a land you control to its owner's hand" — Trench Behemoth.
///
/// Distinct from <see cref="ReturnSelfToHandCostRule"/> (self-bounce) and the effect-position
/// <see cref="ReturnSelfToHandEffectRule"/>: this is a return of a controller-chosen permanent
/// as the activation cost. The returned permanent is a land — identified EITHER by a land subtype
/// (Forest/Island/Swamp/Mountain/Plains and the nonbasic land types, CR 205.3i) OR by the bare
/// card type "land" (CR 300.1 / 205.2a).
///
/// CR 118.3: any game action may be a cost; CR 602: activated abilities have a cost before the
/// colon. The cost here moves the chosen land to its owner's hand as part of paying.
/// ObjectReferenceKind.Any (not Target): the controller selects any qualifying permanent at
/// activation time — not a legal target (Rule 115.1 — "target" requires the word "target").
/// </summary>
[ActivatedCostRule(Priority = 997)]
public sealed class ReturnLandSubtypeToHandCostRule : IActivatedCostRule
{
  // Anchored pattern: "Return a <word> you control to its owner's hand". <word> must be the land
  // card type ("land") or an actual land subtype — see TryMatch's validation. The end anchor +
  // the word validation prevent both substring collisions ("Return this creature…") and the overfit
  // that would write a card-type word ("land") or an unrelated subtype into the Subtypes slot.
  private static readonly Regex _pattern = new(
    @"^Return\s+a\s+(?<word>[A-Za-z]+)\s+you\s+control\s+to\s+its\s+owner's\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Land subtypes (CR 205.3i): the five basics plus the single-word nonbasic land types. "land"
  // itself is NOT here — it is the card type (CR 300.1), handled separately below.
  private static readonly HashSet<string> LandSubtypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "Plains", "Island", "Swamp", "Mountain", "Forest",
    "Desert", "Gate", "Lair", "Locus", "Mine", "Tower", "Cave", "Sphere",
  };

  public Cost? TryMatch(string costText)
  {
    var m = _pattern.Match(costText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var word = m.Groups["word"].Value;

    ObjectFilter filter;
    if (string.Equals(word, "land", System.StringComparison.OrdinalIgnoreCase))
    {
      // "Return a land you control…" — the bare card type (CR 300.1 / 205.2a), NOT a subtype.
      // Writing it into Subtypes (the prior overfit) yielded a malformed {land, Subtypes:[land]}
      // for Trench Behemoth, which has no land subtype "land".
      filter = new ObjectFilter { CardTypes = ["land"], Controller = ControllerFilter.You };
    }
    else if (LandSubtypes.Contains(word))
    {
      filter = new ObjectFilter
      {
        CardTypes = ["land"],
        Subtypes = [word],
        Controller = ControllerFilter.You,
      };
    }
    else
    {
      // Not a land card-type/subtype word (e.g. "creature", "artifact") — this is not a land-return
      // cost. Return null so the correct rule handles it rather than mislabeling it as a land.
      return null;
    }

    // Any — controller picks one qualifying permanent at resolution; no "target" keyword so this
    // is NOT a targeted ability (CR 115.1). ObjectReferenceKind.Any is the correct reference kind
    // for "a [filter] you control" in a cost position.
    return new ReturnToHandCost
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Any, Filter = filter },
    };
  }
}
