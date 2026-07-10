namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// The reciprocal power-fight activated sentences (Karplusan Yeti:
/// "{T}: This creature deals damage equal to its power to target creature.
/// That creature deals damage equal to its power to this creature.").
/// The activated parser's multi-sentence pre-pass splits the effect half on the
/// ". " boundary and dispatches each sentence here individually, so this rule
/// recognises the two surfaces separately:
/// <list type="bullet">
///   <item>"This creature deals damage equal to its power to target creature." —
///   source is <see cref="ObjectReferenceKind.Self"/> (the permanent with the
///   ability); the amount is a <see cref="DerivedQuantity"/> of the source's own
///   power ("its power"); the recipient is a fresh target creature.</item>
///   <item>"That creature deals damage equal to its power to this creature." —
///   source is the previously-targeted creature, referenced back as
///   <see cref="ObjectReferenceKind.ThatCreature"/> (a linked reference, ADR 0004
///   reference-not-resolution, not a threaded binding); the amount is that
///   creature's own power; the recipient is <see cref="ObjectReferenceKind.Self"/>.</item>
/// </list>
///
/// Both emit a <see cref="DealDamageEffect"/> — non-combat damage (CR 120.1: "An
/// object that deals damage is the source of that damage."), left as the default
/// (<c>IsCombat</c> null) because the ability marks no combat context. This is the
/// long-hand of a two-way fight but is NOT the fight keyword action (CR 701.14): only
/// one creature is a target (the other is the source's <see cref="ObjectReferenceKind.Self"/>),
/// and the exchange is written as two ordered damage events rather than a single
/// symmetric <see cref="MagicAST.AST.Effects.Combat.FightEffect"/>.
///
/// Both patterns are anchored <c>^…$</c> on their full surface so neither can claim
/// a substring of any broader sibling sentence.
/// </summary>
[ActivatedEffectRule(Priority = 991)]
public sealed class DealsDamageEqualToPowerReciprocalEffectRule : IActivatedEffectRule
{
  // "This creature deals damage equal to its power to target creature"
  private static readonly Regex SelfToTargetPattern = new(
    @"^This\s+creature\s+deals?\s+damage\s+equal\s+to\s+its\s+power\s+to\s+target\s+creature$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "That creature deals damage equal to its power to this creature"
  private static readonly Regex ThatToSelfPattern = new(
    @"^That\s+creature\s+deals?\s+damage\s+equal\s+to\s+its\s+power\s+to\s+this\s+creature$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    if (SelfToTargetPattern.IsMatch(trimmed))
    {
      return new DealDamageEffect
      {
        Source = ObjectReference.Self(),
        Amount = new DerivedQuantity
        {
          DerivedFrom = DerivedKind.Power,
          Source = "it",
        },
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
      };
    }

    if (ThatToSelfPattern.IsMatch(trimmed))
    {
      return new DealDamageEffect
      {
        Source = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
        Amount = new DerivedQuantity
        {
          DerivedFrom = DerivedKind.Power,
          Source = "it",
        },
        Target = ObjectReference.Self(),
      };
    }

    return null;
  }
}
