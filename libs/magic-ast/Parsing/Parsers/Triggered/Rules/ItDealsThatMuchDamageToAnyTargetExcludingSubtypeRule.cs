namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals that much damage to any target that isn't a [Subtype]." — the
/// damage-redirection effect on Wrathful Red Dragon ("Whenever a Dragon you
/// control is dealt damage, it deals that much damage to any target that isn't a
/// Dragon."). "It" is the triggering permanent (the previously mentioned Dragon,
/// not necessarily this card's own source — see
/// <see cref="SubtypeDealtDamageConditionRule"/>), "that much" is the amount of
/// damage it was just dealt, and "any target that isn't a [Subtype]" is an
/// unrestricted target (creature, player, planeswalker, or battle) excluding
/// members of the named subtype.
///
/// <para>
/// The antecedent of "that much" is the damage dealt in the triggering event
/// (<see cref="DerivedKind.DamageDealt"/>), matching the
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.CreatureDealtDamage"/> trigger.
/// The exclusion is carried on <see cref="ObjectFilter.ExcludedSubtypes"/> (CR
/// 205.3 — subtypes) rather than restricting <see cref="ObjectFilter.CardTypes"/>,
/// since "any target" is not itself narrowed to creatures — only Dragons (a
/// creature subtype) are excluded from an otherwise unrestricted target.
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. … An object that deals damage is the source of that damage." CR
/// 603.2: "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers." This is the "any target
/// excluding a subtype" sibling of
/// <see cref="ItDealsThatMuchDamageToYouRule"/> ("to you", self-restricted
/// trigger) and reuses the same <see cref="DerivedKind.DamageDealt"/> "that much"
/// derivation.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ItDealsThatMuchDamageToAnyTargetExcludingSubtypeRule : ITriggeredRule
{
  // Anchored: "it deals that much damage to any target that isn't a|an
  // <Subtype>." Full-string anchors (^…$) prevent substring collisions with more
  // generic siblings (e.g. plain "to any target").
  private static readonly Regex _pattern = new(
    @"^it\s+deals?\s+that\s+much\s+damage\s+to\s+any\s+target\s+that\s+isn.t\s+an?\s+(?<subtype>[A-Z][A-Za-z]+)\.?$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value;

    effect = new DealDamageEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.DamageDealt },
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.AnyTarget,
        Filter = new ObjectFilter { ExcludedSubtypes = [subtype] },
      },
      Source = ObjectReference.It(),
    };
    return true;
  }
}
