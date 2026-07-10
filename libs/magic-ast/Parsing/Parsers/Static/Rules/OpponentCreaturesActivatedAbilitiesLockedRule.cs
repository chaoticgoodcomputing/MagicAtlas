namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Activated abilities of creatures your opponents control can't be activated." —
/// Linvala, Keeper of Silence's static ability. A continuous restriction
/// (CR 602.5c: activation is prohibited by a static ability) that prevents any
/// activated ability on a creature controlled by an opponent — including mana
/// abilities, since the oracle text carries no "unless they're mana abilities"
/// carve-out — from being put on the stack.
///
/// <para>
/// Modelled as a <see cref="StaticAbility"/> whose <c>AffectedObjects</c> is an
/// <see cref="ObjectFilter"/> scoping the restriction to creatures controlled by
/// opponents (<c>CardTypes=["creature"], Controller=Opponent</c>), and whose
/// <c>Effects</c> list contains a single <see cref="CantActivateAbilitiesEffect"/>
/// with no <c>Target</c> (the affected-objects filter on the ability already names
/// the locked objects) and no <c>ExceptManaAbilities</c> (unset — the lock is
/// total, unlike the Pithing Needle / Sorcerous Spyglass mana-ability carve-out).
/// Mirrors <see cref="OpponentArtifactsActivatedAbilitiesLockedRule"/> with a
/// creature-scoped filter in place of the artifact-scoped one.
/// </para>
///
/// <para>
/// The pattern is anchored (^…$) so it cannot fire as a substring of a longer
/// clause such as a reminder parenthetical, and is disjoint from the
/// artifact-scoped and chosen-name-scoped templates.
/// </para>
/// </summary>
[StaticRule(Priority = 979)]
public sealed class OpponentCreaturesActivatedAbilitiesLockedRule : IStaticRule
{
  // Anchored so it can't match inside a longer clause.
  // Accepts optional trailing period.
  private static readonly Regex _pattern = new(
    @"^\s*Activated\s+abilities\s+of\s+creatures\s+your\s+opponents?\s+control\s+can'?t\s+be\s+activated\.?\s*$",
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
          CardTypes = ["creature"],
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
