namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Tap-a-singular-subtype cost: "Tap an untapped Gate you control",
/// "Tap a untapped Elf you control" — the spelled-out "Tap …" verb used as an
/// activation cost (CR 118.3 / 602.5; CR 701.26 tap/untap) where the count is
/// the indefinite article ("a"/"an") rather than a number word, and the tapped
/// permanent is identified by subtype rather than (or in addition to) card type.
/// e.g. Gateway Shade: "Tap an untapped Gate you control: This creature gets
/// +2/+2 until end of turn."
///
/// <para>
/// Distinct from <see cref="TapSubtypesCostRule"/> (requires an explicit number
/// word "one"/"two"/… before "untapped") — this rule handles the singular
/// article form, which never spells out "one". Distinct from
/// <see cref="TapPermanentsCostRule"/> (requires a literal card-type word —
/// "creature"/"artifact"/"land"/"permanent" — in the cost text); a bare subtype
/// like "Gate" carries no such word.
/// </para>
///
/// <para>
/// The tapped permanent's card type is inferred from the subtype: land subtypes
/// (CR 205.3i — the basics plus common single-word nonbasic land types, mirroring
/// the set used by <see cref="ReturnLandSubtypeToHandCostRule"/>) resolve to
/// <c>CardTypes: ["land"]</c>; any other (capitalised) subtype word is assumed to
/// be a creature subtype (CR 205.3m), matching <see cref="TapSubtypesCostRule"/>'s
/// idiom. This generalizes the emitted <see cref="TapPermanentsCost.Filter"/> beyond
/// the creature-only assumption so land-typed permanents like Gates are modeled
/// correctly rather than mislabeled as creatures.
/// </para>
///
/// <para>
/// The "untapped" qualifier is implicit in any tap cost (CR 701.26a: a permanent
/// must be untapped to be tapped to pay the cost), so it is not encoded as a
/// separate axis on the emitted <see cref="TapPermanentsCost"/> — matching the
/// Conspire / <see cref="TapPermanentsCostRule"/> / <see cref="TapSubtypesCostRule"/>
/// idiom.
/// </para>
///
/// <para>
/// Anchored (^…$) to avoid matching as a substring of a longer/different cost
/// string. Priority 998 — same tier as <see cref="TapSubtypesCostRule"/>, and
/// mutually exclusive with it (that rule requires a number word; this one requires
/// the indefinite article), so ordering between the two does not matter.
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 998)]
public sealed class TapSingularSubtypeCostRule : IActivatedCostRule
{
  // Matches: "Tap a/an untapped [Word] you control"
  // - Word: the subtype (singular) — must begin with uppercase (Rule 205.3m / 205.3i).
  private static readonly Regex _pattern = new(
    @"^Tap\s+an?\s+untapped\s+(?<word>[A-Z][A-Za-z]+)\s+you\s+control$",
    RegexOptions.Compiled
  );

  // Land subtypes (CR 205.3i): the five basics plus single-word nonbasic land types.
  // Mirrors the set in ReturnLandSubtypeToHandCostRule. "land" itself is the card type
  // (CR 300.1), not a subtype, and is not handled here (this rule requires a capitalised
  // subtype word — the bare card-type form is TapPermanentsCostRule's territory).
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

    ObjectFilter filter = LandSubtypes.Contains(word)
      ? new ObjectFilter
        {
          CardTypes = ["land"],
          Subtypes = [word],
          Controller = ControllerFilter.You,
        }
      : new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [word],
          Controller = ControllerFilter.You,
        };

    return new TapPermanentsCost
    {
      Filter = filter,
      Quantity = LiteralQuantity.Of(1),
    };
  }
}
