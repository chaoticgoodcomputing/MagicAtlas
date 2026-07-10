namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Other creatures you control of the chosen type get +X/+Y." — the creature-source
/// sibling of <see cref="ChosenTypeAnthemModifyPTRule"/> (Adaptive Automaton shape,
/// e.g. paired with "As this creature enters, choose a creature type." and "This
/// creature is the chosen type in addition to its other types."). CR 613.1: an
/// object's characteristics are recomputed in layers, the anthem being a
/// P/T-modifying continuous effect scoped by the creature type chosen as this
/// permanent entered.
///
/// <para>The "of the chosen type" clause is the structured consumer half of a CR 607
/// linked ability — CR 607.1: "An object may have two abilities printed on it such
/// that one of them causes actions to be taken or objects or players to be affected
/// and the other one directly refers to those actions, objects, or players." The
/// producer is the "choose a creature type" effect under
/// <c>StaticAbility.When = AsThisEnters</c>. It is encoded with the structured
/// <see cref="ObjectFilter.ChosenCharacteristic"/> = <see cref="ChosenCharacteristicKind.CreatureType"/>
/// reference, never free-text.</para>
///
/// <para>Distinct from <see cref="ChosenTypeAnthemModifyPTRule"/>: that rule's source
/// is a noncreature permanent (e.g. an enchantment), so it never appears in its own
/// affected set and no "Other " prefix is printed. Here the source IS a creature (and
/// per <see cref="IsChosenCreatureTypeInAdditionRule"/> is itself the chosen type), so
/// the printed "Other " prefix excludes it from its own anthem (CR 611.3;
/// <see cref="ObjectFilter.ExcludeSelf"/> = true) — mirrors
/// <see cref="TribalAnthemModifyPTRule"/>'s "Other [Subtype] creatures" shape but with
/// a runtime-chosen subtype instead of a printed one.</para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class OtherChosenTypeAnthemModifyPTRule : IStaticRule
{
  private static readonly Regex _otherChosenTypeAnthemModifyPTPattern = new(
    @"^\s*Other\s+creatures\s+you\s+control\s+of\s+the\s+chosen\s+type\s+get\s+\+(?<p>\d+)/\+(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _otherChosenTypeAnthemModifyPTPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
              ChosenCharacteristic = ChosenCharacteristicKind.CreatureType,
              ExcludeSelf = true,
            },
          },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }
}
