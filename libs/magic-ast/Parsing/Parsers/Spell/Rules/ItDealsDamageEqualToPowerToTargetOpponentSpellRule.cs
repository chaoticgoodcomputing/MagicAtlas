namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the pronoun-sourced "bite" continuation sentence:
///   "It deals damage equal to its power to target creature an opponent controls."
///
/// This is the back-reference companion of <see cref="BiteRule"/> (which handles the
/// self-contained "Target creature you control deals damage equal to its power …"
/// single-sentence form). Here the damage source is a previously-mentioned creature —
/// the "It"/"its" both refer to the creature the preceding sentence pumped (Ambuscade's
/// "Target creature you control gets +1/+0 …. It deals damage …") — so the source is
/// an <see cref="ObjectReferenceKind.It"/> reference rather than a fresh target.
///
/// Emits a <see cref="DealDamageEffect"/> (non-combat damage, CR 120.1 — the source is
/// the object dealing the damage) whose amount is a <see cref="DerivedQuantity"/> of the
/// source's power ("equal to its power") and whose target is an opponent-controlled
/// creature ("an opponent controls" → <see cref="ControllerFilter.Opponent"/>).
///
/// Anchored <c>^…$</c> on the "It deals damage equal to its power to target creature an
/// opponent controls" surface so it cannot swallow any broader sibling.
///
/// Example:
/// <list type="bullet">
///   <item>"It deals damage equal to its power to target creature an opponent controls."  (Ambuscade — second sentence)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class ItDealsDamageEqualToPowerToTargetOpponentSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^It\s+deals\s+damage\s+equal\s+to\s+its\s+power\s+to\s+target\s+creature\s+an\s+opponent\s+controls$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim().TrimEnd('.')))
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Source = new ObjectReference { Kind = ObjectReferenceKind.It },
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Power,
        Source = "it",
      },
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.Opponent,
        },
      },
    };
    return true;
  }
}
