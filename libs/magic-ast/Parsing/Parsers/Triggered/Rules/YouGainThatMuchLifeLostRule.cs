namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you gain that much life" where "that much" is the life a player just LOST —
/// the gain-life half of a <see cref="MagicAST.AST.Triggers.TriggerEvent.LosesLife"/>
/// trigger (Exquisite Blood, Bloodthirsty Conqueror).
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." The antecedent of "that much"
/// is the amount of life lost in the triggering event, so the gained amount is a
/// <see cref="DerivedQuantity"/> keyed on <see cref="DerivedKind.LifeLost"/>.
/// </para>
///
/// <para>
/// The effect surface ("you gain that much life") is identical to the lifelink/
/// damage family handled by <see cref="YouGainThatMuchLifeRule"/>, which keys the
/// derived amount on <see cref="DerivedKind.DamageDealt"/>. The two differ ONLY by
/// the triggering event — the antecedent of "that much" is the trigger's own
/// event. This rule is therefore NOT reflection-discovered into the generic
/// effect loop (it carries no <c>[TriggeredRule]</c>); the
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/> dispatches to it
/// directly only when the resolved trigger event is <c>LosesLife</c>, so the two
/// "that much" surfaces never collide.
/// </para>
/// </summary>
public sealed class YouGainThatMuchLifeLostRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^you\s+gain\s+that\s+much\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new GainLifeEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.LifeLost },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
