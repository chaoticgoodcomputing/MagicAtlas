namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;

/// <summary>
/// "Cast this spell only during the declare attackers step and only if you've been
/// attacked this step." — a card-inherent casting-TIME restriction combining a named
/// phase gate (Declare Attackers Step, CR 508) with an intervening condition (you've
/// been attacked this step). The combat-trick "…Stand" family (Warrior's Stand,
/// Defiant Stand, Harsh Justice).
///
/// <para>
/// Modelled as a <see cref="TimingModificationEffect"/> with
/// <see cref="TimingModificationType.Restrict"/> at <see cref="TimingWindow.Phase"/>
/// ("declare attackers step"), gated by <see cref="BeenAttackedThisStepCondition"/>.
/// <c>WhoseTurn</c> is deliberately left null — being attacked happens on an
/// OPPONENT's turn, not "yours".
/// </para>
///
/// <para>
/// Descriptive-not-engine / reference-not-resolution (ADR 0004): MAST records the
/// restriction and the gate; the engine enforces the casting-time legality and does
/// NOT pre-resolve the condition to a boolean.
/// </para>
///
/// CR 601.3a (verbatim): "If an effect prohibits a player from casting a spell with
/// certain qualities, that player may consider any choices to be made during that
/// spell's proposal that may cause those qualities to change. If any such choices
/// could cause that effect to no longer prohibit that player from casting that spell,
/// the player may begin to cast the spell, ignoring the effect." — the printed
/// restriction is such an effect; a spell may be begun to be cast only if no
/// rule/effect prohibits it.
/// CR 508.1 (verbatim): "First, the active player declares attackers. This turn-based
/// action doesn't use the stack. To declare attackers, the active player follows the
/// steps below, in order. ..."
/// Glossary "Declare Attackers Step" (verbatim): "Part of the turn. This step is the
/// second step of the combat phase. See rule 508, 'Declare Attackers Step.'"
/// </summary>
[SpellRule]
public sealed class CastOnlyDuringDeclareAttackersIfAttackedSpellRule : ISpellRule
{
  // Anchored whole-line; tolerant of straight (') and curly (’) apostrophe in
  // "you've" and of an optional trailing period. Mutually exclusive with siblings
  // (default Priority 50).
  private static readonly Regex Pattern = new(
    @"^Cast\s+this\s+spell\s+only\s+during\s+the\s+declare\s+attackers\s+step\s+and\s+only\s+if\s+you['’]ve\s+been\s+attacked\s+this\s+step\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new TimingModificationEffect
    {
      Modification = TimingModificationType.Restrict,
      Timing = TimingWindow.Phase,
      Phase = "declare attackers step",
      Condition = new BeenAttackedThisStepCondition(),
    };
    return true;
  }
}
