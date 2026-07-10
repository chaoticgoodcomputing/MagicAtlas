namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target creature with no counters on it." — destroys a single creature
/// gated on carrying <em>no counters of any kind</em> (Heartless Act, mode 1).
///
/// <para>
/// The "no counters on it" clause is a board-state predicate: the creature qualifies
/// only while it has zero counters of any type (CR 122.1 — "A counter is a marker
/// placed on an object or player that modifies its characteristics and/or interacts
/// with a rule or effect."). MAST records it on the target filter as a
/// <see cref="CounterCharacteristic"/> whose <see cref="CounterCharacteristic.Count"/>
/// is a <c>= 0</c> comparison, with the sentinel <c>CounterType = "any"</c> denoting
/// "counters of any kind" (as opposed to the printed-type values like "+1/+1" or the
/// existing "all" sentinel for "every counter type present on a source"). "any" + a
/// zero count reads as "has zero counters of any kind" = "no counters on it".
/// Reference-not-resolution (ADR 0004): the engine reads the target's live counter
/// state at targeting/resolution; MAST records the predicate as written.
/// </para>
///
/// <para>
/// Anchored to the full "creature with no counters on it" surface so it cannot shadow
/// the generic <see cref="DestroyTargetSimpleRule"/> / <see cref="DestroyTargetWithFilterRule"/>
/// (both of which reject this multi-word "with …" phrase anyway). Priority 80 places it
/// ahead of those default-priority (50) siblings for deterministic dispatch.
/// </para>
/// </summary>
[SpellRule(Priority = 80)]
public sealed class DestroyTargetCreatureWithNoCountersRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+creature\s+with\s+no\s+counters\s+on\s+it$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics =
          [
            new CounterCharacteristic
            {
              CounterType = "any",
              Count = new Comparison
              {
                Operator = ComparisonOperator.Equal,
                Value = 0,
              },
            },
          ],
        },
      },
    };
    return true;
  }
}
