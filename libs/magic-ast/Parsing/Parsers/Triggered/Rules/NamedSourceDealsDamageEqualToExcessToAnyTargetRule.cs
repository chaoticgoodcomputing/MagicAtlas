namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[CardName] deals damage equal to the excess to any target [other than that permanent]."
/// — named-source ping where the damage amount equals the excess from an
/// <see cref="TriggerEvent.ExcessNoncombatDamageDealt"/> trigger (KHM: Toralf, God of
/// Fury). The subject starts with a capital letter (a card name, CR 201.4: a card's name
/// in its own text refers to itself). MAST resolves the self-reference to
/// <see cref="ObjectReferenceKind.Self"/>.
///
/// <para>
/// The amount "equal to the excess" is modelled as
/// <c>DerivedQuantity { DerivedFrom = DerivedKind.DamageDealt }</c>: the triggering
/// event's excess is the distinct quantum of damage dealt beyond lethal (CR 120.10 —
/// "excess damage equal to the difference"). <c>DamageDealt</c> is the canonical
/// derived-quantity axis for damage dealt in a triggering event, reused here for excess
/// damage since there is no separate <c>ExcessDamage</c> kind.
/// </para>
///
/// <para>
/// The trailing "other than that permanent" exclusion is dropped: no
/// <see cref="ObjectReferenceKind"/> carries a "ThatPermanent" exclusion; the engine
/// resolves the targeting constraint via the existing AnyTarget + CR 115.7 legality.
/// This matches the precedent in PawpatchRecruit ("target creature you control other than
/// that creature" → unfiltered creature target).
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent matching inside a broader clause.
/// Priority 61 — just above <see cref="NamedSourceDealsDamageToAnyTargetTriggeredRule"/>
/// (Priority 60) so the "equal to the excess" shape is claimed before the literal-amount
/// form attempts to parse the non-numeric "equal to the excess" amount.
/// </para>
/// </summary>
[TriggeredRule(Priority = 61)]
public sealed class NamedSourceDealsDamageEqualToExcessToAnyTargetRule : ITriggeredRule
{
  // "[CardName] deals damage equal to the excess to any target [other than that permanent]."
  // The "[other than that permanent]" suffix is optional and always dropped.
  private static readonly Regex _pattern = new(
    @"^(?<subject>[A-Z]\S.*?)\s+deals?\s+damage\s+equal\s+to\s+the\s+excess\s+to\s+any\s+target(?:\s+other\s+than\s+that\s+permanent)?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    // Subject must begin with a capital letter — card names are capitalised (CR 201.4).
    var subject = m.Groups["subject"].Value;
    if (!char.IsUpper(subject[0]))
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.DamageDealt },
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
    return true;
  }
}
