namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals that much damage to any target." — the plain, unrestricted-target
/// sibling of <see cref="ItDealsThatMuchDamageToAnyTargetExcludingSubtypeRule"/>
/// ("... to any target that isn't a [Subtype].") and
/// <see cref="ItDealsThatMuchDamageToYouRule"/> ("... to you."). Blazing
/// Sunsteel: "Whenever equipped creature is dealt damage, it deals that much
/// damage to any target." "It" is the triggering equipped creature (see
/// <see cref="EquippedCreatureDealtDamageConditionRule"/>), not the Equipment
/// itself, so the source is <see cref="ObjectReferenceKind.It"/> rather than
/// <see cref="ObjectReferenceKind.Self"/> — matching how
/// <see cref="ItDealsThatMuchDamageToAnyTargetExcludingSubtypeRule"/> resolves
/// "it" for the analogous "a Dragon you control is dealt damage" trigger.
///
/// <para>
/// The antecedent of "that much" is the damage dealt in the triggering event
/// (<see cref="DerivedKind.DamageDealt"/>), matching the
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.CreatureDealtDamage"/> trigger.
/// "Any target" (CR 115.4) is unrestricted — a creature, player, planeswalker, or
/// battle — carried with no <see cref="ObjectReference.Filter"/>.
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. … An object that deals damage is the source of that damage." CR
/// 603.2: "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers."
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ItDealsThatMuchDamageToAnyTargetRule : ITriggeredRule
{
  // Anchored: "it deals that much damage to any target." Full-string anchors
  // (^…$) prevent substring collisions with the more-specific excluding-subtype
  // sibling (which requires a trailing "that isn't a|an <Subtype>" clause).
  private static readonly Regex _pattern = new(
    @"^it\s+deals?\s+that\s+much\s+damage\s+to\s+any\s+target\.?$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
      Source = ObjectReference.It(),
    };
    return true;
  }
}
