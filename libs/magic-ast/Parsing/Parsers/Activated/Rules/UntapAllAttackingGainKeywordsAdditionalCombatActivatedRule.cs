namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Untap all attacking creatures. They gain [kw, kw, and kw] until end of turn. After this
/// phase, there is an additional combat phase." — Najeela, the Blade-Blossom's 5-color combat
/// engine. A three-clause composite whose second clause's subject "They" is a back-reference to
/// the attacking creatures untapped by the first clause; both resolve to "each attacking
/// creature" (CR 508 — attacking creatures), so the grant target mirrors the untap target
/// (reference-not-resolution, ADR 0004). The trailing clause inserts an additional combat phase
/// (CR 506 / CR 500.8 — adding phases), reusing <see cref="AdditionalCombatPhaseEffect"/>.
///
/// <para>
/// Whole-body composite (mirrors the triggered
/// <c>TryParseGainEnergyThenMayPayEnergyUntapAllControlledAndAdditionalCombat</c>): the middle
/// "They gain …" clause has no card-independent meaning (its "They" needs the untap clause's
/// subject), so it is NOT split into a standalone effect rule. Recognized here as one unit so
/// "They" is bound to the attacking-creatures set in-context. Exposed via
/// <see cref="IMultiActivatedEffectRule.TryMatchMulti"/> (the single-effect
/// <see cref="IActivatedEffectRule.TryMatch"/> returns null); the parser's multi-sentence
/// pre-pass declines this body (the "Untap all attacking creatures" and "They gain …" clauses
/// have no standalone rules), falling through to the multi-rule path where this fires. Anchored
/// (^…$).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 60)]
public sealed class UntapAllAttackingGainKeywordsAdditionalCombatActivatedRule
  : IActivatedEffectRule,
    IMultiActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Untap\s+all\s+attacking\s+creatures\.\s+They\s+gain\s+(?<kws>.+?)\s+until\s+end\s+of\s+turn\.\s+After\s+this\s+phase,?\s+there\s+is\s+an\s+additional\s+combat\s+phase\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText) => null;

  private static ObjectReference AttackingCreatures() =>
    new()
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Characteristics = [Characteristic.InCombat(CombatState.Attacking)],
      },
    };

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(effectText.Trim());
    if (!m.Success)
    {
      return false;
    }

    var list = new List<Effect> { new UntapEffect { Target = AttackingCreatures() } };

    // Split "trample, lifelink, and haste" (oxford comma) into individual keywords.
    var keywordNames = Regex.Split(
      m.Groups["kws"].Value.Trim(),
      @"\s*,\s*and\s+|\s*,\s*|\s+and\s+",
      RegexOptions.IgnoreCase
    );
    foreach (var name in keywordNames)
    {
      if (string.IsNullOrWhiteSpace(name))
      {
        continue;
      }
      var ability = ActivatedRuleHelpers.BuildGrantedKeywordAbility(name.Trim());
      if (ability is null)
      {
        // Unrecognised keyword — bail so the fallback parser handles the card.
        return false;
      }
      list.Add(new GainAbilityEffect
      {
        Target = AttackingCreatures(),
        GainedAbility = ability,
        Duration = UntilTimeDuration.EndOfTurn,
      });
    }

    list.Add(new AdditionalCombatPhaseEffect());

    effects = list;
    return true;
  }
}
