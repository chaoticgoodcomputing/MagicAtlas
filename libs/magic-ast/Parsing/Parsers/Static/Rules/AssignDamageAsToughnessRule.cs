namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// "Each creature you control assigns combat damage equal to its toughness rather
/// than its power." — High Alert, Doran-the-Siege-Tower family.
///
/// Rule 510.1a (verbatim): "Each attacking creature and each blocking creature
/// assigns combat damage equal to its power." This static ability establishes a
/// global replacement that substitutes toughness for power in the combat-damage
/// assignment step, governed by CR 614 (replacement effects) and CR 613 (layer
/// ordering).
///
/// <para>
/// The pattern is fully anchored (^ … $) to prevent substring-matching another
/// oracle line that contains this phrase embedded inside a larger clause.
/// </para>
/// </summary>
[StaticRule(Priority = 956)]
public sealed class AssignDamageAsToughnessRule : IStaticRule
{
  // Handles the "each creature you control" subject form.
  // Anchored; matches only the standalone oracle line.
  private static readonly Regex _eachControlledPattern = new(
    @"^\s*Each\s+creature\s+you\s+control\s+assigns\s+combat\s+damage\s+equal\s+to\s+its\s+toughness\s+rather\s+than\s+its\s+power\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Handles the "all creatures" global form (e.g. Doran, the Siege Tower).
  // Anchored; mutually exclusive with the "you control" arm above.
  private static readonly Regex _allCreaturesPattern = new(
    @"^\s*All\s+creatures\s+assign\s+combat\s+damage\s+equal\s+to\s+their\s+toughness\s+rather\s+than\s+their\s+power\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Handles the self form: "This creature assigns combat damage equal to its
  // toughness rather than its power."
  private static readonly Regex _selfPattern = new(
    @"^\s*This\s+creature\s+assigns\s+combat\s+damage\s+equal\s+to\s+its\s+toughness\s+rather\s+than\s+its\s+power\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var raw = clause.RawText;

    if (_eachControlledPattern.IsMatch(raw))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new AssignDamageAsToughnessEffect
            {
              AppliesTo = new ObjectReference
              {
                Kind = ObjectReferenceKind.Each,
                Filter = new ObjectFilter
                {
                  CardTypes = ["creature"],
                  Controller = ControllerFilter.You,
                },
              },
            },
          ],
        },
      ];
    }

    if (_allCreaturesPattern.IsMatch(raw))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new AssignDamageAsToughnessEffect
            {
              AppliesTo = new ObjectReference
              {
                Kind = ObjectReferenceKind.Each,
                Filter = new ObjectFilter { CardTypes = ["creature"] },
              },
            },
          ],
        },
      ];
    }

    if (_selfPattern.IsMatch(raw))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new AssignDamageAsToughnessEffect()],
        },
      ];
    }

    return null;
  }
}
