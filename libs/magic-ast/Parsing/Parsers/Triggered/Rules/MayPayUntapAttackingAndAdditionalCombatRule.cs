namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "you may pay {COST}. If you do, untap all attacking creatures and after this phase,
/// there is an additional combat phase." — the Hellkite Charger combat-reset pattern.
///
/// <para>
/// Produces an <see cref="OptionalEffect"/> whose <c>Inner</c> is a
/// <see cref="ConditionalPayEffect"/> carrying the mana cost, and whose
/// <c>IfYouDo</c> is a <see cref="CompositeEffect"/> containing:
/// <list type="bullet">
///   <item>An <see cref="UntapEffect"/> targeting all attacking creatures
///   (CR 701.26: untap; CR 508: attacking creatures).</item>
///   <item>An <see cref="AdditionalCombatPhaseEffect"/> (CR 500.8: adding phases
///   to a turn).</item>
/// </list>
/// </para>
///
/// <para>
/// Pattern is anchored (^...$) to prevent matching as a substring of a more-specific
/// sibling. Only the COST is variable; "untap all attacking creatures" and
/// "after this phase, there is an additional combat phase" are fixed. This rule
/// deliberately handles only this exact clause shape rather than extending the
/// general <see cref="ConditionalPayTriggeredRule"/> to avoid broad combinatorial
/// coupling.
/// </para>
///
/// <para>
/// CR references: CR 500.8 (adding a phase to a turn); CR 508.3a (attacks trigger);
/// CR 701.26 (tap/untap); CR 117.3 (paying costs optionally).
/// </para>
/// </summary>
[TriggeredRule(Priority = 85)]
public sealed class MayPayUntapAttackingAndAdditionalCombatRule : ITriggeredRule
{
  // Anchored: must be the entire effect text. Captures the mana cost symbols
  // between the two mandatory phrases. Priority 85 fires before the generic
  // ConditionalPayTriggeredRule (Priority 80) so the more-specific shape wins.
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\.\s*If\s+you\s+do,\s*untap\s+all\s+attacking\s+creatures\s+and\s+after\s+this\s+phase,\s+there\s+is\s+an\s+additional\s+combat\s+phase\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(m.Groups["cost"].Value);
    if (manaCost is null)
    {
      return false;
    }

    // "untap all attacking creatures" — Each creature with CombatState.Attacking.
    // No controller filter: the oracle text says "all attacking creatures" (any
    // attacking creature, not just those you control). CR 508: a creature is
    // attacking if declared as an attacker this combat.
    var untapTarget = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Characteristics = [Characteristic.InCombat(CombatState.Attacking)],
      },
    };

    var ifYouDo = new CompositeEffect
    {
      Effects =
      [
        new UntapEffect { Target = untapTarget },
        new AdditionalCombatPhaseEffect(),
      ],
    };

    effect = new OptionalEffect
    {
      Inner = new ConditionalPayEffect { Cost = manaCost },
      IfYouDo = ifYouDo,
    };

    return true;
  }
}
