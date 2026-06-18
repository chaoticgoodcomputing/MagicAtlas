namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Activated abilities of artifacts your opponents control can't be activated." —
/// Karn, the Great Creator's static ability. A continuous restriction (CR 604.2 /
/// CR 611.1) that prevents any activated ability on an artifact controlled by an
/// opponent from being put on the stack (CR 602.5: "A player can't begin to activate
/// an ability that's prohibited from being activated.").
///
/// <para>
/// Modelled as a <see cref="StaticAbility"/> whose <c>AffectedObjects</c> is an
/// <see cref="ObjectFilter"/> scoping the restriction to artifacts controlled by
/// opponents (<c>CardTypes=["artifact"], Controller=Opponent</c>), and whose
/// <c>Effects</c> list contains a single <see cref="CantActivateAbilitiesEffect"/>
/// with no <c>Target</c> (the affected-objects filter on the ability already names
/// the locked objects). This matches the pattern used for global activation locks
/// (cf. <see cref="EnchantedCantAttackOrBlockRule"/> for the per-Aura form where
/// Target is set to <c>EnchantedOrEquipped</c>).
/// </para>
///
/// <para>
/// The pattern is anchored (^…$) so it cannot fire as a substring of a longer
/// clause such as a reminder parenthetical.
/// </para>
/// </summary>
[StaticRule(Priority = 979)]
public sealed class OpponentArtifactsActivatedAbilitiesLockedRule : IStaticRule
{
  // Anchored so it can't match inside a longer clause.
  // Accepts optional trailing period.
  private static readonly Regex _pattern = new(
    @"^\s*Activated\s+abilities\s+of\s+artifacts\s+your\s+opponents?\s+control\s+can'?t\s+be\s+activated\.?\s*$",
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
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["artifact"],
          Controller = ControllerFilter.Opponent,
        },
        Effects =
        [
          new CantActivateAbilitiesEffect(),
        ],
      },
    ];
  }
}
