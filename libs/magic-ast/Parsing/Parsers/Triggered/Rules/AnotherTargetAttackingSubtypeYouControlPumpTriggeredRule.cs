namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "another target attacking [Subtype] you control gets +N/+M until end of
/// turn." — the effect clause of an attack trigger that pumps a chosen
/// ATTACKING creature of a specific subtype the controller controls, other
/// than the source (Fervent Champion: "Whenever this creature attacks, another
/// target attacking Knight you control gets +1/+0 until end of turn.").
///
/// <para>
/// CR 603.1: the trigger condition and the effect are separate composable
/// nodes; this rule handles only the effect clause (the trigger side is owned
/// by <see cref="AttacksConditionRule"/>). CR 611.2a / CR 514.2: an "until end
/// of turn" continuous effect ends during cleanup — modelled as
/// <see cref="UntilTimeDuration.EndOfTurn"/>.
/// </para>
///
/// <para>
/// The filter carries four axes on top of the bare
/// <see cref="ModifyPTTargetAttackingCreatureEffectRule"/> shape ("target
/// attacking creature gets…"): a single capitalised subtype token
/// (<see cref="ObjectFilter.Subtypes"/>, CR 205.3m), <c>Controller = You</c>
/// ("you control", CR 109.5), <c>ExcludeSelf = true</c> ("another" — the
/// codebase convention mapping "another" to <see cref="ObjectFilter.ExcludeSelf"/>,
/// mirrored from <see cref="AnotherTargetAttackingCreatureCantBeBlockedThisTurnRule"/>
/// and <see cref="AnotherTargetControlledCreatureGainsKeywordUntilEndOfTurnRule"/>),
/// and the combat-state predicate <see cref="Characteristic.InCombat"/> with
/// <see cref="CombatState.Attacking"/> ("attacking").
/// </para>
///
/// <para>
/// Anchored (^…$) and requires the capitalised subtype token immediately
/// between "attacking" and "you control", so it cannot substring-match the
/// generic-noun sibling <c>^another target attacking creature can't be
/// blocked…</c> surface, nor <see cref="OtherSubtypePumpTriggeredRule"/>'s
/// untargeted, plural "other [Subtype]s you control get…" mass-pump shape.
/// </para>
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class AnotherTargetAttackingSubtypeYouControlPumpTriggeredRule : ITriggeredRule
{
  // "another target attacking <CapitalisedSubtype> you control gets +N/+M until end of turn"
  private static readonly Regex _pattern = new(
    @"^another\s+target\s+attacking\s+(?<subtype>[A-Z][a-zA-Z]+)\s+you\s+control\s+gets\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)\s+until\s+end\s+of\s+turn\.?$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value;
    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
          ExcludeSelf = true,
          Characteristics = [Characteristic.InCombat(CombatState.Attacking)],
        },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
