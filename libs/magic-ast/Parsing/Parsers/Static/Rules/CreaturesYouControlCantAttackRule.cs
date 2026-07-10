namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// Board-wide, permanent (not "this turn") attacker-side restriction filtered by
/// CONTROL: "Creatures you control can't attack." (Glacial Chasm). CR 508.1c
/// (declare-attackers step; attacking restrictions constrain the set of legal
/// attacker declarations the active player can make).
///
/// <para>
/// Sibling of <see cref="CreaturesWithoutKeywordCantAttackRule"/> ("Creatures
/// without flying can't attack.", Moat), which filters the restricted set by
/// keyword-absence; here the restricted set is filtered by
/// <see cref="ObjectFilter.Controller"/> = <see cref="ControllerFilter.You"/>
/// instead. Maps to a <see cref="CantAttackEffect"/> whose
/// <see cref="CantAttackEffect.Target"/> is <see cref="ObjectReferenceKind.Each"/>
/// creature filtered by <c>CardTypes=["creature"], Controller=You</c>. No
/// <see cref="MagicAST.AST.Effects.Duration"/> — the restriction is always-on,
/// not scoped to the current turn.
/// </para>
///
/// <para>
/// ANCHORED (^…$) on the literal "Creatures you control can't attack." template
/// so it cannot collide as a substring of a broader clause and cannot claim the
/// "without &lt;keyword&gt;" or self-referencing ("This creature"/"Enchanted"/
/// "Equipped") sibling shapes, which use distinct wording.
/// </para>
/// </summary>
[StaticRule(Priority = 960)]
public sealed class CreaturesYouControlCantAttackRule : IStaticRule
{
  // "Creatures you control can't attack." — anchored on the full literal clause so
  // this cannot collide with the "without <keyword>" sibling (CreaturesWithoutKeywordCantAttackRule)
  // or any other "can't attack" shape.
  private static readonly Regex _pattern = new(
    @"^\s*Creatures\s+you\s+control\s+can'?t\s+attack\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

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
          new CantAttackEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
            },
          },
        ],
      },
    ];
  }
}
