namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target creature gets -X/-X until end of turn, where X is the number of cards
/// revealed this way." — the payoff sentence of the Scent of Nightshade family, paired
/// with <see cref="RevealAnyNumberBlackCardsFromHandRule"/> (the reveal that supplies X).
///
/// <para>
/// X = the number of cards revealed this way (<see cref="CardsRevealedThisWayQuantity"/>).
/// The "-X" modifier is that count negated, modeled with the established negation
/// convention (Dread Slag's "-N/-N for each …"): a <see cref="CalculatedQuantity"/> whose
/// <see cref="CalculatedQuantity.BaseQuantity"/> is the revealed count, multiplied by -1.
/// This is a distinct sentence from the reveal, so it does not reference the reveal at
/// runtime — the link is textual (ADR 0004 reference-not-resolution).
/// </para>
///
/// <para>
/// This rule sits above the generic <see cref="ModifyPTSpellRule"/> (whose anchored
/// patterns require literal-digit modifiers and forbid a trailing "where …" clause, so
/// they never match this shape); the priority bump is defensive precedence only.
/// </para>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class TargetCreatureGetsMinusXRevealedThisWayRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+gets\s+-X/-X\s+until\s+end\s+of\s+turn,\s*where\s+X\s+is\s+the\s+number\s+of\s+cards\s+revealed\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    // -X = (cards revealed this way) × -1 — the Dread Slag negation convention.
    Quantity NegatedRevealedCount() =>
      new CalculatedQuantity
      {
        BaseQuantity = new CardsRevealedThisWayQuantity(),
        Operation = "multiply",
        Operand = -1,
      };

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      PowerModifier = NegatedRevealedCount(),
      ToughnessModifier = NegatedRevealedCount(),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
