namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Creatures you control of the chosen type get +X/+Y." — a continuous P/T anthem
/// (CR 613.1: an object's characteristics are recomputed in layers, the anthem being
/// a P/T-modifying continuous effect) whose affected set is filtered by the creature
/// type chosen as this permanent entered.
///
/// <para>The "of the chosen type" clause is the structured consumer half of a CR 607
/// linked ability — CR 607.1: "An object may have two abilities printed on it such
/// that one of them causes actions to be taken or objects or players to be affected
/// and the other one directly refers to those actions, objects, or players. If so,
/// these two abilities are linked: the second refers only to actions that were taken
/// or objects or players that were affected by the first, and not by any other
/// ability." The producer is the "choose a creature type" effect under
/// <c>StaticAbility.When = AsThisEnters</c>. It is encoded with the structured
/// <see cref="ObjectFilter.ChosenCharacteristic"/> = <see cref="ChosenCharacteristicKind.CreatureType"/>
/// reference, never free-text.</para>
///
/// <para>Distinct from <see cref="TribalAnthemModifyPTRule"/>, whose affected set is a
/// fixed named subtype ("Other Goblin creatures you control get …"). Here the subtype
/// is runtime-chosen rather than printed, so the subtype axis is the chosen-characteristic
/// reference, not <c>Subtypes</c>. No "Other " prefix — the source enchantment is not a
/// creature, so it never appears in the affected set.</para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class ChosenTypeAnthemModifyPTRule : IStaticRule
{
  private static readonly Regex _chosenTypeAnthemModifyPTPattern = new(
    @"^\s*Creatures\s+you\s+control\s+of\s+the\s+chosen\s+type\s+get\s+\+(?<p>\d+)/\+(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chosenTypeAnthemModifyPTPattern.Match(clause.RawText);
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
            },
          },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }
}
