namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target creature deals damage to itself equal to its power."
///
/// <para>
/// The target creature is both the damage source and the damage recipient.
/// The amount is a <see cref="DerivedQuantity"/> with <see cref="DerivedKind.Power"/>,
/// sourced from the same object ("it"). Covers Repentance (WL Sorcery) and
/// Justice Strike (GRN Instant).
/// </para>
///
/// <para>
/// Distinguished from <see cref="SelfDealsDamageToAnyTargetRule"/> (spell is the
/// source, not the creature) and from combat-damage rules (this is a non-combat
/// damage event resolving at sorcery/instant speed).
/// </para>
/// </summary>
[SpellRule]
public sealed class TargetDealsDamageToSelfRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+creature\s+deals\s+damage\s+to\s+itself\s+equal\s+to\s+its\s+power$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    var targetCreature = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter { CardTypes = ["creature"] },
    };

    effect = new DealDamageEffect
    {
      Source = targetCreature,
      Target = ObjectReference.It(),
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Power,
        Source = "it",
      },
    };
    return true;
  }
}
