namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Activated abilities of artifacts can't be activated." — Collector Ouphe's static
/// ability. A continuous restriction (CR 604.2 / CR 611.1) that prevents any activated
/// ability on any artifact — regardless of controller — from being put on the stack
/// (CR 602.5: "A player can't begin to activate an ability that's prohibited from being
/// activated.").
///
/// <para>
/// Modelled as a <see cref="StaticAbility"/> whose <c>AffectedObjects</c> is an
/// <see cref="ObjectFilter"/> scoping the restriction to artifacts
/// (<c>CardTypes=["artifact"]</c>, no <c>Controller</c> — the lock is symmetric, unlike
/// Karn, the Great Creator's opponent-scoped form), and whose <c>Effects</c> list
/// contains a single <see cref="CantActivateAbilitiesEffect"/> with no <c>Target</c>
/// (the affected-objects filter on the ability already names the locked objects). This
/// mirrors <see cref="OpponentArtifactsActivatedAbilitiesLockedRule"/> minus the
/// controller qualifier.
/// </para>
///
/// <para>
/// The pattern is anchored (^…$) so it cannot fire as a substring of a longer clause,
/// and it does not match the "your opponents control" variant (that phrase makes the
/// text longer than this pattern permits, so
/// <see cref="OpponentArtifactsActivatedAbilitiesLockedRule"/> stays the sole match for
/// that card).
/// </para>
/// </summary>
[StaticRule(Priority = 979)]
public sealed class ArtifactsActivatedAbilitiesLockedRule : IStaticRule
{
  // Anchored so it can't match inside a longer clause. Accepts optional trailing period.
  private static readonly Regex _pattern = new(
    @"^\s*Activated\s+abilities\s+of\s+artifacts\s+can'?t\s+be\s+activated\.?\s*$",
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
        AffectedObjects = new ObjectFilter { CardTypes = ["artifact"] },
        Effects =
        [
          new CantActivateAbilitiesEffect(),
        ],
      },
    ];
  }
}
