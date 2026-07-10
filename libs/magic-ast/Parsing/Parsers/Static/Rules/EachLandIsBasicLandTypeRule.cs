namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the Blanket-of-Night oracle template: a static continuous effect that
/// additively grants a single basic land type to every land, "in addition to its
/// other land types."
///
/// <para>
/// Oracle text (verbatim): "Each land is a Swamp in addition to its other land
/// types."
/// </para>
///
/// <para>
/// This is a layer-4 (CR 613.1d) continuous type-changing effect. Per CR 305.7:
/// "If a land gains one or more land types in addition to its own, it keeps its
/// land types and rules text, and it gains the new land types and mana abilities."
/// Because the sentence says "in addition to its other land types" (not a bare
/// "is a Swamp"), CR 205.1a governs: the land's prior types are retained and the
/// stated basic land type is appended — an <see cref="AddTypeEffect"/>, never a
/// replacing <c>SetCardTypesEffect</c>/<c>ChangeSubtypeEffect</c>.
/// </para>
///
/// <para>
/// CR 205.1a (verbatim): "Some effects change an object's card type, subtype,
/// and/or supertype but specify that the object retains a prior card type,
/// subtype, and/or supertype. In such cases, all the object's prior card types,
/// subtypes, and supertypes are retained, and the effect causes the object to
/// gain or lose other card types, subtypes, and/or supertypes."
/// </para>
///
/// <para>
/// CR 305.6 (verbatim): "The basic land types are Plains, Island, Swamp,
/// Mountain, and Forest. If an object uses the words 'basic land type,' it's
/// referring to one of these subtypes."
/// </para>
///
/// <para>
/// Distinct from <see cref="LandsAreEveryBasicLandTypeRule"/> (Dryad of the
/// Ilysian Grove: a controller-scoped "Lands you control are every basic land
/// type" grant of all five types) and <see cref="NonlandCreatureTypeGrantRule"/>
/// (Ashaya: creatures gain a land card type + subtype). Here the subject is
/// EVERY land (no controller restriction — "Each land") and the granted subtype
/// is a single named basic land type.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full oracle sentence is matched exactly so this rule
/// cannot fire as a substring match against a more specific sibling. Priority
/// 971 — matching the sibling "are every basic land type" shape's priority; the
/// two patterns are mutually exclusive by construction (singular "Each land is"
/// vs. "Lands you control are every basic land type") so there is no collision.
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class EachLandIsBasicLandTypeRule : IStaticRule
{
  // Anchored full-sentence match:
  // "Each land is a Swamp in addition to its other land types."
  private static readonly Regex _pattern = new(
    @"^\s*Each\s+land\s+is\s+an?\s+(?<type>Plains|Island|Swamp|Mountain|Forest)\s+in\s+addition\s+to\s+its\s+other\s+land\s+types\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // The unrestricted land filter: "Each land" (every land, any controller).
  private static readonly ObjectReference _eachLand = new()
  {
    Kind = ObjectReferenceKind.Each,
    Filter = new ObjectFilter
    {
      CardTypes = ["land"],
    },
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    // Canonicalize to PascalCase (regex alternation already constrains to the
    // five basic land type spellings, case-insensitively).
    var raw = m.Groups["type"].Value;
    var canonical = char.ToUpperInvariant(raw[0]) + raw[1..].ToLowerInvariant();

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new AddTypeEffect
          {
            Target = _eachLand,
            AddedSubtypes = [canonical],
          },
        ],
      },
    ];
  }
}
