namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "put that many +1/+1 counters on this creature" — counter-placement effect
/// where the count is derived from life just GAINED by the triggering player.
/// Backs the <see cref="MagicAST.AST.Triggers.TriggerEvent.GainsLife"/> trigger
/// on Sunbond: "Enchanted creature has &quot;Whenever you gain life, put that
/// many +1/+1 counters on this creature.&quot;"
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." CR 122.1: "A counter is a
/// marker placed on an object or player that modifies its characteristics
/// and/or interacts with a rule or effect." The antecedent of "that many" is
/// the amount of life just gained in the triggering event, keyed on
/// <see cref="DerivedKind.LifeGained"/> — the same derived-quantity axis used
/// by <see cref="LoseLifeDerivedRule"/> for the sibling "loses that much life"
/// surface (Vito, Thorn of the Dusk Rose).
/// </para>
///
/// <para>
/// "This creature" resolves to <see cref="ObjectReferenceKind.Self"/> — the
/// object bearing the ability (CR 109.5), which becomes the enchanted creature
/// once the ability is granted by the Aura (CR 113.6: an ability granted to an
/// object by an effect is a fully-fledged ability of that object). Mirrors the
/// plain "on this creature" → Self mapping used by
/// <see cref="PutCountersTriggeredRule"/>, but with a DERIVED count instead of
/// a literal one.
/// </para>
///
/// <para>
/// This rule is NOT reflection-discovered into the generic effect loop (no
/// <c>[TriggeredRule]</c> attribute); <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/>
/// dispatches to it directly only when the resolved trigger event is
/// <c>GainsLife</c> — mirroring <see cref="DrawThatManyCardsLifeLostRule"/>'s
/// LosesLife-guarded wiring — so the "put that many … counters" surface is
/// never resolved against this LifeGained antecedent under a DIFFERENT
/// trigger event (e.g. a hypothetical damage- or counter-placement-triggered
/// card sharing the same effect-text idiom with a different antecedent).
/// </para>
/// </summary>
public sealed class PutThatManyPlusOneCountersGainsLifeRule : ITriggeredRule
{
  // "put that many +1/+1 counters on this creature[.]" — anchored end-to-end.
  private static readonly Regex Pattern = new(
    @"^put\s+that\s+many\s+\+1/\+1\s+counters?\s+on\s+this\s+creature\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new PutCountersEffect
    {
      Target = ObjectReference.Self(),
      CounterType = "+1/+1",
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.LifeGained },
    };
    return true;
  }
}
