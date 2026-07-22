namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "target attacking creature without flying gains flying until end of turn." —
/// Oteclan Levitator's attack trigger. Grants flying, until end of turn, to a
/// single targeted creature constrained to two axes both already first-class on
/// <see cref="ObjectFilter"/>:
/// <list type="bullet">
///   <item>"attacking" — a <see cref="CombatStateCharacteristic"/> with
///   <see cref="CombatState.Attacking"/> (CR 508; the combat-state carve-out from the
///   <see cref="OtherCharacteristic"/> residual).</item>
///   <item>"without flying" — <see cref="ObjectFilter.LacksKeywords"/> = [Flying]
///   (CR 702.9; the same negative-keyword axis Moat uses).</item>
/// </list>
///
/// <para>The grant is the standard "gains [keyword] until end of turn" shape
/// (<see cref="GainAbilityEffect"/> with <see cref="UntilTimeDuration.EndOfTurn"/>),
/// building the flying static ability via
/// <see cref="TriggeredRuleHelpers.BuildKeywordStaticAbility"/> — identical to the
/// grant <see cref="ItGainsKeywordUntilEndOfTurnRule"/> produces, differing only in
/// the target reference (an explicit filtered "target attacking creature without
/// flying" rather than the anaphoric "it").</para>
///
/// <para>ANCHORED (^…$): the full clause is anchored so it cannot substring-match a
/// sibling. Narrow to the flying/flying wording Oteclan Levitator prints.</para>
/// </summary>
[TriggeredRule(Priority = 62)]
public sealed class TargetAttackingCreatureWithoutFlyingGainsFlyingRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^target\s+attacking\s+creature\s+without\s+flying\s+gains\s+flying\s+until\s+end\s+of\s+turn\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    var flying = TriggeredRuleHelpers.BuildKeywordStaticAbility("flying");
    if (flying is null)
    {
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [new CombatStateCharacteristic { State = CombatState.Attacking }],
          LacksKeywords = [KeywordAbility.Flying],
        },
      },
      GainedAbility = flying,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
