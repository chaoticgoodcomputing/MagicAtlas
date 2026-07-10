namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the "bite" spell whose damage recipient is phrased with the
/// <c>"an opponent controls"</c> controller clause rather than the
/// <c>"you don't control"</c> clause handled by <see cref="BiteRule"/>:
/// <list type="bullet">
///   <item>"Target creature you control deals damage equal to its power to target creature an opponent controls."  (Rocky Rebuke)</item>
///   <item>"Target creature you control deals damage equal to its power to target creature or planeswalker an opponent controls."</item>
/// </list>
///
/// Both clauses denote the same set of objects — a permanent an opponent controls —
/// so this emits the identical <see cref="DealDamageEffect"/> shape as <see cref="BiteRule"/>:
/// <list type="bullet">
///   <item>Source is the controlled target creature (<see cref="ControllerFilter.You"/>).</item>
///   <item>Amount is a <see cref="DerivedQuantity"/> of Power sourced from "it" ("equal to its power").</item>
///   <item>Target is an opponent-controlled creature (<see cref="ControllerFilter.Opponent"/>),
///         with an optional "or planeswalker" broadening the target's card types.</item>
/// </list>
///
/// Non-combat damage (CR 120.1 — the source creature is the object dealing the damage; there is
/// no combat, so <see cref="DealDamageEffect.IsCombat"/> stays null). This is the asymmetric
/// "bite", distinct from the symmetric <see cref="FightRule"/> (CR 701.14): the recipient does
/// not deal damage back.
///
/// Anchored <c>^…$</c> on the full sentence so it can only match this exact effect surface and
/// cannot swallow any broader or more-specific sibling.
/// </summary>
[SpellRule]
public sealed class BiteOpponentControlsSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+creature\s+you\s+control\s+deals\s+damage\s+equal\s+to\s+its\s+power\s+to\s+target\s+creature(?:\s+or\s+(?<extra>planeswalker))?\s+an\s+opponent\s+controls$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var cardTypes = m.Groups["extra"].Success
      ? new[] { "creature", m.Groups["extra"].Value.ToLowerInvariant() }
      : new[] { "creature" };

    effect = new DealDamageEffect
    {
      Source = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Power,
        Source = "it",
      },
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Controller = ControllerFilter.Opponent,
        },
      },
    };
    return true;
  }
}
