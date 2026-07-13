namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals that much damage to you." — the reflexive-punishment effect on
/// Jackal Pup ("Whenever this creature is dealt damage, it deals that much damage
/// to you."). "It" is the creature itself (the trigger's source), "that much" is
/// the amount of damage the creature was just dealt, and "you" is its controller.
///
/// <para>
/// The antecedent of "that much" is the damage dealt in the triggering event
/// (<see cref="DerivedKind.DamageDealt"/>), matching the
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.CreatureDealtDamage"/> trigger.
/// The target "you" is the controller of the source (CR 109.5: "The words 'you'
/// and 'your' on an object refer to the object's controller …"), carried as
/// <see cref="ObjectReferenceKind.You"/>; the source is the creature itself,
/// carried on <c>Source</c> as <see cref="ObjectReferenceKind.Self"/>.
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. … An object that deals damage is the source of that damage." CR 603.2:
/// "Whenever a game event or game state matches a triggered ability's trigger
/// event, that ability automatically triggers." This is the "to you" sibling of
/// <see cref="DealThatMuchDamageToControllerRule"/> ("to that creature's
/// controller") and the "that much" sibling of <see cref="SelfDealsDamageToYouRule"/>
/// (which handles a literal amount "deals N damage to you").
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ItDealsThatMuchDamageToYouRule : ITriggeredRule
{
  // Anchored: the source noun phrase is "it" or "this <permanent-word>"; the tail
  // "deals that much damage to you" is the invariant. Full-string anchors (^…$)
  // prevent substring collisions with more-specific siblings (e.g. "to that
  // creature's controller", "to each other opponent").
  private static readonly Regex _pattern = new(
    @"^(?:it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+that\s+much\s+damage\s+to\s+you\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.DamageDealt },
      Target = ObjectReference.You(),
      Source = ObjectReference.Self(),
    };
    return true;
  }
}
