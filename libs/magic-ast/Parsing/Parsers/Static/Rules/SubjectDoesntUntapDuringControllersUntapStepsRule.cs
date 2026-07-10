namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "[Lands/Creatures/Artifacts/Permanents] don't untap during their controllers'
/// untap steps." — a static continuous effect (CR 502.3) that suppresses the
/// untap-step untapping of an entire class of permanents, regardless of who
/// controls them or the ability's source. Canonical card: Hokori, Dust Drinker.
///
/// <para>
/// Distinct from <see cref="DoesntUntapRule"/>'s self-reference and
/// enchanted/equipped forms: those key the affected object off the ability's own
/// source ("This [type]" / "Enchanted [type]"), a single object. This rule keys
/// the affected object off a bare card-type noun with no controller qualifier —
/// the effect applies to every permanent of that type, across every player's
/// battlefield — so the subject is modelled as an <see cref="ObjectReference"/>
/// with <see cref="ObjectReferenceKind.Each"/> over an unqualified
/// <see cref="ObjectFilter.CardTypes"/> filter, not a single <c>Self</c>/
/// <c>EnchantedOrEquipped</c> target.
/// </para>
///
/// <para>
/// Generalizes over the subject noun (Lands / Creatures / Artifacts /
/// Permanents) — the four card types that commonly carry this template — rather
/// than a card-literal or "Lands"-literal regex, so a sibling like "Creatures
/// don't untap during their controllers' untap steps." is covered by the same
/// rule.
/// </para>
///
/// <para>
/// Also generalizes over the five basic land SUBTYPES (Islands / Swamps /
/// Plains / Mountains / Forests), which key an unqualified
/// <see cref="ObjectFilter.Subtypes"/> filter rather than
/// <see cref="ObjectFilter.CardTypes"/> — mirroring how "Destroy all Islands."
/// models its target as <c>Subtypes:["Island"]</c> with no card-type filter.
/// Canonical card: Choke ("Islands don't untap during their controllers'
/// untap steps.").
/// </para>
///
/// <para>
/// Also generalizes over an optional leading COLOR qualifier on the four
/// card-type nouns — "Red creatures don't untap during their controllers'
/// untap steps." (Wrath of Marit Lage) — which adds an
/// <see cref="ObjectFilter.Colors"/> constraint alongside the
/// <see cref="ObjectFilter.CardTypes"/> filter rather than replacing it,
/// mirroring how "Destroy target green or white creature." keys
/// <c>Colors</c> and <c>CardTypes</c> together on one filter (CR 105). The
/// color qualifier does not apply to the basic land subtype forms (a bare
/// land subtype like "Islands" carries no color axis of its own).
/// </para>
///
/// <para>
/// CR 502.3 (verbatim): "Third, the active player determines which permanents
/// they control will untap. Then they untap them all simultaneously. This
/// turn-based action doesn't use the stack. Normally, all of a player's
/// permanents untap, but effects can keep one or more of a player's permanents
/// from untapping."
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full oracle sentence is matched exactly so this rule
/// cannot fire on a substring of a more specific sibling clause.
/// </para>
/// </summary>
[StaticRule(Priority = 973)]
public sealed class SubjectDoesntUntapDuringControllersUntapStepsRule : IStaticRule
{
  // Anchored full-sentence match:
  // "[<Color>? Lands|Creatures|Artifacts|Permanents|Islands|Swamps|Plains|
  // Mountains|Forests] don't untap during their controllers' untap steps."
  // Apostrophe class tolerates both the straight (U+0027) and curly (U+2019)
  // apostrophe forms used across printings. Case-insensitive on the subject
  // noun: it is capitalized at the start of a bare sentence ("Creatures
  // don't...", Hokori) but lowercase when preceded by a color qualifier
  // ("Red creatures don't...", Wrath of Marit Lage) since only the leading
  // word of the sentence is capitalized.
  private static readonly Regex _pattern = new(
    @"^\s*(?:(?<color>White|Blue|Black|Red|Green)\s+)?(?<subject>Lands|Creatures|Artifacts|Permanents|Islands|Swamps|Plains|Mountains|Forests)\s+don[’']t\s+untap\s+during\s+their\s+controllers[’']?\s+untap\s+steps\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  private static readonly IReadOnlyDictionary<string, string> _subjectToCardType =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Lands"] = "land",
      ["Creatures"] = "creature",
      ["Artifacts"] = "artifact",
      ["Permanents"] = "permanent",
    };

  // Color-name surface form → single-letter WUBRG code (CR 105.1).
  private static readonly IReadOnlyDictionary<string, string> _colorNameToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["White"] = "W",
      ["Blue"] = "U",
      ["Black"] = "B",
      ["Red"] = "R",
      ["Green"] = "G",
    };

  // Basic land subtypes (plural surface form → singular subtype string), keyed
  // separately from card types since these subjects filter on
  // ObjectFilter.Subtypes, not ObjectFilter.CardTypes (mirrors "Destroy all
  // Islands." → Filter:{Subtypes:["Island"]}).
  private static readonly IReadOnlyDictionary<string, string> _subjectToBasicLandSubtype =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Islands"] = "Island",
      ["Swamps"] = "Swamp",
      ["Plains"] = "Plains",
      ["Mountains"] = "Mountain",
      ["Forests"] = "Forest",
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var subject = match.Groups["subject"].Value;
    var colorGroup = match.Groups["color"];

    ObjectFilter filter;
    if (_subjectToCardType.TryGetValue(subject, out var cardType))
    {
      string[]? colors = null;
      if (colorGroup.Success && _colorNameToCode.TryGetValue(colorGroup.Value, out var colorCode))
      {
        colors = [colorCode];
      }

      filter = new ObjectFilter { CardTypes = [cardType], Colors = colors };
    }
    else if (!colorGroup.Success && _subjectToBasicLandSubtype.TryGetValue(subject, out var subtype))
    {
      // A color qualifier never applies to the bare basic-land-subtype form
      // (see doc comment): "Red Islands" is not a template this rule covers.
      filter = new ObjectFilter { Subtypes = [subtype] };
    }
    else
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new DoesntUntapEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = filter,
            },
            WhoseUntapStep = "their controllers'",
          },
        ],
      },
    ];
  }
}
