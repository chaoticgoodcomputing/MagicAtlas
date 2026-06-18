namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "draw that many cards" (imperative form) — draw effect where the count is
/// derived from life just LOST by the triggering player. Backs the
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.LosesLife"/> trigger on
/// Vilis, Broker of Blood: "Whenever you lose life, draw that many cards."
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." The antecedent of "that many"
/// is the amount of life lost in the triggering event, keyed on
/// <see cref="DerivedKind.LifeLost"/>.
/// </para>
///
/// <para>
/// Distinct from <see cref="YouDrawThatManyCardsTriggeredRule"/> (which handles
/// the "you draw that many cards" surface and keys the count on
/// <see cref="DerivedKind.DamageDealt"/>): Vilis's effect text omits the "you"
/// subject (imperative form) and the triggering event is life loss, not damage
/// dealt. This rule is NOT reflection-discovered into the generic effect loop
/// (no <c>[TriggeredRule]</c> attribute); the
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/> dispatches to
/// it directly only when the resolved trigger event is
/// <c>LosesLife</c>, preventing the "that many" surface from ever being
/// resolved as <c>DamageDealt</c> under a life-loss trigger.
/// </para>
/// </summary>
public sealed class DrawThatManyCardsLifeLostRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^draw\s+that\s+many\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DrawCardsEffect
    {
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.LifeLost },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
