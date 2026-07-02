namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "If target player has fewer than nine poison counters, they get a number of
/// poison counters equal to the difference." — Vraska, Betrayal's Sting's −9
/// loyalty ability.
///
/// <para>
/// A <see cref="ConditionalEffect"/> whose condition is an
/// <see cref="OtherCondition"/> ("target player has fewer than nine poison counters")
/// and whose <c>Then</c> branch is a <see cref="PutCountersEffect"/> giving that
/// player a <see cref="CalculatedQuantity"/> equal to <c>"the difference"</c>
/// (i.e. 9 minus the current poison counter count — engine territory per the
/// descriptive-not-engine doctrine; MAST records the reference, not the resolved
/// value).
/// </para>
///
/// <para>
/// CR 122 (counters); CR 704.5c: "If a player has ten or more poison counters,
/// that player loses the game." The ability targets the player (CR 115.6); the
/// target reference is <see cref="ObjectReferenceKind.Target"/> with a
/// <c>CardTypes = ["player"]</c> filter.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the phrase "fewer than nine poison counters" does not
/// appear in any other sibling rule, but anchoring prevents accidental substring
/// matches.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 954)]
public sealed class PoisonCounterDifferenceEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^If\s+target\s+player\s+has\s+fewer\s+than\s+nine\s+poison\s+counters?,\s+they\s+get\s+a\s+number\s+of\s+poison\s+counters?\s+equal\s+to\s+the\s+difference$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    var targetPlayer = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter { CardTypes = ["player"] },
    };

    // "They" back-references the targeted player.
    var they = new ObjectReference { Kind = ObjectReferenceKind.It };

    return new ConditionalEffect
    {
      Condition = Condition.Other("target player has fewer than nine poison counters"),
      Then = new PutCountersEffect
      {
        Target = they,
        CounterType = "poison",
        Count = new CalculatedQuantity { Expression = "the difference" },
      },
    };
  }
}
