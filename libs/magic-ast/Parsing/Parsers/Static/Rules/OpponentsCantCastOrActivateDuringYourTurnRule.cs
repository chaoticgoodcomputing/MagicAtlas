namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "During your turn, your opponents can't cast spells or activate abilities of
/// artifacts, creatures, or enchantments." (Myrel, Shield of Argive) — a
/// continuous static prohibition scoped two ways at once, mirroring
/// <see cref="OpponentsCantCastDuringYourTurnRule"/>: WHO is prohibited (a player
/// class — "your opponents") and WHEN the prohibition is in effect (a timing
/// window — "during your turn"), except the timing qualifier LEADS the sentence
/// here rather than trailing it, and the sentence bundles TWO prohibitions
/// joined by "or" rather than one.
///
/// <para>
/// CR 601.3a: a spell can't be cast if doing so would violate a rule or effect
/// that says a player can't cast a spell. CR 602.5c: a player can't begin to
/// activate an ability that's prohibited from being activated. Both halves are
/// therefore emitted as TWO separate <see cref="StaticAbility"/> nodes sharing
/// the same "During your turn" <see cref="Condition"/> rather than one ability
/// with two effects: each prohibition scopes a DIFFERENT class of affected
/// objects (spells vs. artifacts/creatures/enchantments) via
/// <see cref="StaticAbility.AffectedObjects"/>, and that filter lives once per
/// ability — it cannot carry two different <c>CardTypes</c> sets at once.
/// </para>
///
/// <para>
/// Cast half reuses <see cref="CantBeCastEffect"/> exactly as
/// <see cref="OpponentsCantCastDuringYourTurnRule"/> does: the WHO half rides on
/// <see cref="ObjectFilter.Controller"/> = <see cref="ControllerFilter.Opponent"/>
/// (a spell's controller is the player casting it, CR 108.4a), with
/// <c>CardTypes=["spell"]</c>.
/// </para>
///
/// <para>
/// Activate half reuses <see cref="CantActivateAbilitiesEffect"/> exactly as
/// <see cref="OpponentArtifactsActivatedAbilitiesLockedRule"/> does for Karn, the
/// Great Creator's single-type form: <c>CardTypes=["artifact","creature","enchantment"]</c>
/// (OR semantics per CR 115.3, mirroring <see cref="ReturnUpToOneTargetTypeDisjunctionToHandEffectRule"/>'s
/// established type-disjunction convention) with <c>Controller=Opponent</c> —
/// the default activator of a permanent's activated ability is its controller
/// (CR 602.5c), so scoping the affected permanents to those opponents control is
/// the structural equivalent of restricting the opponents themselves as actors,
/// exactly as the cast half does for spells.
/// </para>
///
/// <para>
/// Both abilities carry the same "During your turn" <see cref="OtherCondition"/>
/// (the same PB-7 structured-condition bucket <c>SelfKeywordDuringTurnSuffixRule</c>
/// and <see cref="OpponentsCantCastDuringYourTurnRule"/> use), produced via the
/// shared <see cref="MagicAST.Parsing.ConditionParser"/> entry point.
/// </para>
///
/// Anchored (^…$) so the subject phrase cannot match as a substring of a more
/// specific sibling static line.
/// </summary>
[StaticRule(Priority = 972)]
public sealed class OpponentsCantCastOrActivateDuringYourTurnRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*During\s+your\s+turn\s*,\s*your\s+opponents\s+can'?t\s+cast\s+spells\s+or\s+activate\s+abilities\s+of\s+artifacts\s*,\s*creatures\s*,\s*or\s+enchantments\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    var duringYourTurn = MagicAST.Parsing.ConditionParser.Parse("During your turn");

    return
    [
      new StaticAbility
      {
        Effects = [new CantBeCastEffect()],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          Controller = ControllerFilter.Opponent,
        },
        Condition = duringYourTurn,
      },
      new StaticAbility
      {
        Effects = [new CantActivateAbilitiesEffect()],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["artifact", "creature", "enchantment"],
          Controller = ControllerFilter.Opponent,
        },
        Condition = duringYourTurn,
      },
    ];
  }
}
