namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Draw a card for each tapped creature target opponent controls." — the controller
/// draws one card per tapped creature the targeted opponent controls (Borrowing
/// 100,000 Arrows).
///
/// <para>
/// CR 121.1 (draw): a card draw is a plain <see cref="DrawCardsEffect"/> with
/// <see cref="ObjectReference.You"/> as the drawing player. The amount is a
/// <see cref="CountQuantity"/> over tapped creatures the targeted opponent controls:
/// <c>CountOf: { CardTypes: ["creature"], Controller: Target, Characteristics: [{
/// CharacteristicType: "tapped", Tapped: true }] }</c>, mirroring the established
/// "for each tapped [type] opponent(s) control" shape in
/// <see cref="AddManaForEachTappedLandOpponentsControlRule"/> (CR 110.5a — a permanent
/// is tapped if it's been turned sideways) and the targeted-opponent-controller shape
/// in <see cref="DealDamageToEachCreatureTargetPlayerRule"/> /
/// <see cref="AddManaForEachCardInHandRule"/> (<see cref="ControllerFilter.Target"/>
/// records that the controller axis is the runtime-chosen target rather than the
/// spell's own controller or a static "opponent" axis; the targeting requirement
/// itself is carried by the parent target reference on the spell, per those
/// precedents — no separate Targets list is needed since the target is fully
/// described inside the filter).
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent substring collisions with sibling "draw a card for
/// each …" clauses that count different object classes.
/// </para>
/// </summary>
[SpellRule]
public sealed class DrawCardsForEachTappedCreatureTargetOpponentControlsRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Draw\s+a\s+card\s+for\s+each\s+tapped\s+creature\s+target\s+opponent\s+controls$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim().TrimEnd('.')))
    {
      return false;
    }

    effect = new DrawCardsEffect
    {
      Count = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.Target,
          Characteristics = [new TappedStateCharacteristic { Tapped = true }],
        },
      },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
