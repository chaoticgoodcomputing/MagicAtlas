namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[source] deals that much damage to that creature's controller" — the
/// Repercussion effect: a named source deals damage equal to the amount just dealt
/// to the triggering creature, targeting that creature's controller.
///
/// <para>
/// The antecedent of "that much" is the damage dealt to the creature in the
/// triggering event (<see cref="DerivedKind.DamageDealt"/>), matching the
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.CreatureDealtDamage"/> trigger
/// (CR 120 — Damage; CR 603.2). The target is the controller of the creature that
/// was dealt damage (<see cref="ObjectReferenceKind.Controller"/>); the source is
/// the enchantment itself (<see cref="ObjectReferenceKind.Self"/>).
/// </para>
///
/// <para>
/// CR 120.1: "Any time an object … would be dealt damage, that damage is dealt to
/// the appropriate target." CR 120.3: damage can be dealt to players. This
/// triggered effect redirects an equivalent amount to the damaged creature's
/// controller, who is a player (not the creature). The source is "this
/// enchantment" — a self-reference carried on the <c>Source</c> field rather than
/// inferred, so the interact-oracle operator can distinguish the damage source.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class DealThatMuchDamageToControllerRule : ITriggeredRule
{
  // Matches "[this enchantment|it] deals that much damage to that creature's controller"
  // Anchors at word boundaries; the source noun phrase may vary (e.g. "it", "this
  // enchantment", or a card name). We match the most common oracle surface and guard
  // on the structural "that much damage to that creature's controller" tail, which is
  // the invariant portion.
  private static readonly Regex _pattern = new(
    @"^(?:this\s+\w+|it)\s+deals?\s+that\s+much\s+damage\s+to\s+that\s+creature's\s+controller\.?$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.Controller },
      Source = ObjectReference.Self(),
    };
    return true;
  }
}
