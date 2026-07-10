namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the Harbinger-of-the-Seas oracle template: a static continuous effect that
/// SETS every nonbasic land's land type to a single named basic land type.
///
/// <para>
/// Oracle text (verbatim): "Nonbasic lands are Islands."
/// </para>
///
/// <para>
/// This is a layer-4 (CR 613.1d) continuous type-changing effect. Unlike
/// <see cref="EachLandIsBasicLandTypeRule"/> (Blanket of Night: "Each land is a Swamp
/// <em>in addition to</em> its other land types" — additive, no "in addition" clause
/// here means the land's prior types are replaced, not retained. CR 205.1a: "when an
/// effect sets one or more of an object's subtypes, the new subtype(s) replaces any
/// existing subtypes from the appropriate set (creature types, land types, …)." So
/// this emits a SET <see cref="ChangeSubtypeEffect"/>, never the additive
/// <see cref="AddTypeEffect"/>. Per CR 305.7 the affected lands lose their old land
/// types and rules text and gain the appropriate mana ability for the new basic land
/// type; MAST records the subtype set and leaves that consequence to the engine.
/// </para>
///
/// <para>
/// The subject "Nonbasic lands" is scoped on the structured supertype-negation axis
/// (CR 205.4c: "Any land with the supertype 'basic' is a basic land. Any land that
/// doesn't have this supertype is a nonbasic land, even if it has a basic land
/// type."), mirroring <c>DestroyTargetNonbasicLandEffectRule</c>'s
/// <c>CardTypes=["land"]</c> + <c>ExcludedSupertypes=["Basic"]</c> encoding — hence
/// this filter EXCLUDES basic lands from the affected set (unlike the unrestricted
/// "Each land" subject of <see cref="EachLandIsBasicLandTypeRule"/>).
/// </para>
///
/// <para>
/// CR 305.6 (verbatim): "The basic land types are Plains, Island, Swamp, Mountain,
/// and Forest. If an object uses the words 'basic land type,' it's referring to one
/// of these subtypes."
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full oracle sentence is matched exactly so this rule cannot
/// fire as a substring match against a more specific sibling. Priority 971 —
/// matching the sibling "Each land is [type] in addition" shape's priority; the two
/// patterns are mutually exclusive by construction ("Nonbasic lands are" vs. "Each
/// land is … in addition to its other land types") so there is no collision.
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class NonbasicLandsAreBasicLandTypeRule : IStaticRule
{
  // Anchored full-sentence match: "Nonbasic lands are Islands."
  private static readonly Regex _pattern = new(
    @"^\s*Nonbasic\s+lands\s+are\s+(?<type>Plains|Islands|Swamps|Mountains|Forests)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // The nonbasic-land filter: "Nonbasic lands" (every land lacking the Basic supertype).
  private static readonly ObjectReference _eachNonbasicLand = new()
  {
    Kind = ObjectReferenceKind.Each,
    Filter = new ObjectFilter
    {
      CardTypes = ["land"],
      ExcludedSupertypes = ["Basic"],
    },
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    // The five basic land types are plural-invariant except "Plains" (already
    // plural in form); canonicalize the matched plural noun to the singular
    // PascalCase subtype name used elsewhere (Subtypes convention).
    var raw = m.Groups["type"].Value;
    var canonical = string.Equals(raw, "Plains", StringComparison.OrdinalIgnoreCase)
      ? "Plains"
      : char.ToUpperInvariant(raw[0]) + raw[1..^1].ToLowerInvariant();

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ChangeSubtypeEffect
          {
            Target = _eachNonbasicLand,
            Subtypes = [canonical],
          },
        ],
      },
    ];
  }
}
