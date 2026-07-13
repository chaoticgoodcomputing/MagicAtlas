namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "This creature is the chosen type in addition to its other types." — the CONSUMER
/// half of the "As this creature enters, choose a creature type." producer
/// (<see cref="ChooseCreatureTypeOnEntryRule"/>; the Unclaimed-Territory-adjacent
/// "choose a creature type" template, e.g. Titan of Littjara). CR 607.1 linked
/// abilities: "An object may have two abilities printed on it such that one of them
/// causes actions to be taken or objects or players to be affected and the other one
/// directly refers to those actions, objects, or players." — here the producer
/// "chooses" a creature type as the permanent enters and this SEPARATE static ability
/// grants that same creature type to the permanent, additively.
///
/// <para>
/// CR 205.1b governs the "in addition to its other types" retention: existing subtypes
/// (and card types) are kept, and the object also gains the new subtype — an additive
/// grant, not a replacement (contrast <see cref="ChangeSubtypeEffect"/>, which SETS/
/// replaces the subtype for "This creature becomes the creature type of your choice").
/// The granted value is the chosen characteristic itself
/// (<see cref="AddTypeEffect.ChosenSubtypeReference"/> = <see cref="ChosenCharacteristicKind.CreatureType"/>),
/// not a literal subtype string, since the value is only known once the paired
/// as-enters choice resolves.
/// </para>
///
/// <para>
/// Sibling of <see cref="ChooseCreatureTypeOnEntryRule"/>: the two regexes are disjoint
/// ("choose a creature type" vs "is the chosen type in addition to its other types"),
/// so dispatch priority relative to that rule is immaterial.
/// </para>
/// </summary>
[StaticRule(Priority = 941)]
public sealed class IsChosenCreatureTypeInAdditionRule : IStaticRule
{
  private static readonly Regex _isChosenCreatureTypeInAdditionPattern = new(
    @"^\s*This\s+(?:permanent|land|creature|artifact|enchantment)\s+is\s+the\s+chosen\s+type\s+in\s+addition\s+to\s+its\s+other\s+types\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _isChosenCreatureTypeInAdditionPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new AddTypeEffect
        {
          Target = ObjectReference.Self(),
          ChosenSubtypeReference = ChosenCharacteristicKind.CreatureType,
        }],
      },
    ];
  }
}
