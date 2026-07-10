namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target creature gets +X/+X until end of turn, where X is the number of cards
/// revealed this way." — the payoff sentence of the Scent of Ivy family, paired with
/// <see cref="RevealAnyNumberGreenCardsFromHandRule"/> (the reveal that supplies X). The
/// positive-modifier mirror of Scent of Nightshade's "-X/-X" clause parsed by
/// <see cref="TargetCreatureGetsMinusXRevealedThisWayRule"/>.
///
/// <para>
/// X = the number of cards revealed this way (<see cref="CardsRevealedThisWayQuantity"/>),
/// used directly as the "+X" modifier (no negation, unlike the "-X" sibling). This is a
/// distinct sentence from the reveal, so it does not reference the reveal at runtime —
/// the link is textual (ADR 0004 reference-not-resolution).
/// </para>
///
/// <para>
/// This rule sits above the generic <see cref="ModifyPTSpellRule"/> (whose anchored
/// patterns require literal-digit modifiers and forbid a trailing "where …" clause, so
/// they never match this shape); the priority bump is defensive precedence only.
/// </para>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class TargetCreatureGetsPlusXRevealedThisWayRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+gets\s+\+X/\+X\s+until\s+end\s+of\s+turn,\s*where\s+X\s+is\s+the\s+number\s+of\s+cards\s+revealed\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      PowerModifier = new CardsRevealedThisWayQuantity(),
      ToughnessModifier = new CardsRevealedThisWayQuantity(),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
