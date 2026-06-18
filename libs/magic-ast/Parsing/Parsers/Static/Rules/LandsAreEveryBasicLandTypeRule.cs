namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the Dryad-of-the-Ilysian-Grove oracle template: a static continuous
/// effect that additively grants every basic land type to all lands the controller
/// controls, "in addition to their other types."
///
/// <para>
/// Oracle text (verbatim):
/// "Lands you control are every basic land type in addition to their other types."
/// </para>
///
/// <para>
/// This is a layer-4 (CR 613.1d) continuous type-changing effect. Per CR 305.7:
/// "If a land gains one or more land types in addition to its own, it keeps its
/// land types and rules text, and it gains the new land types and mana abilities."
/// The five basic land types (CR 305.6: Plains, Island, Swamp, Mountain, Forest)
/// are all granted additively via an <see cref="AddTypeEffect"/> with
/// <see cref="AddTypeEffect.AddedSubtypes"/> containing all five.
/// </para>
///
/// <para>
/// CR 305.6 (verbatim): "The basic land types are Plains, Island, Swamp, Mountain,
/// and Forest. If an object uses the words 'basic land type,' it's referring to one
/// of these subtypes."
/// </para>
///
/// <para>
/// CR 305.2 (verbatim): "A player can normally play one land during their turn;
/// however, continuous effects may increase this number." (context for the first
/// ability on the Dryad, handled by <see cref="PlayAdditionalLandRule"/>).
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full oracle sentence is matched exactly to prevent this rule
/// from firing on any substring of a more-specific sibling. Priority 971 — above
/// <see cref="NonlandCreatureTypeGrantRule"/> (970) since this shape is dedicated
/// to the "every basic land type" template and the controller-scoped land subject.
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class LandsAreEveryBasicLandTypeRule : IStaticRule
{
  // Anchored full-sentence match:
  // "Lands you control are every basic land type in addition to their other types."
  private static readonly Regex _pattern = new(
    @"^\s*Lands\s+you\s+control\s+are\s+every\s+basic\s+land\s+type\s+in\s+addition\s+to\s+their\s+other\s+types\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // All five basic land subtypes per CR 305.6.
  private static readonly IReadOnlyList<string> _allBasicLandTypes =
    ["Plains", "Island", "Swamp", "Mountain", "Forest"];

  /// <summary>
  /// The controller-scoped land filter: "Lands you control".
  /// </summary>
  private static readonly ObjectReference _landsYouControl = new()
  {
    Kind = ObjectReferenceKind.Each,
    Filter = new ObjectFilter
    {
      CardTypes = ["land"],
      Controller = ControllerFilter.You,
    },
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new AddTypeEffect
          {
            Target = _landsYouControl,
            AddedSubtypes = _allBasicLandTypes,
          },
        ],
      },
    ];
  }
}
