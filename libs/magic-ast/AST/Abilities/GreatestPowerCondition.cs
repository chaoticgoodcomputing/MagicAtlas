namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "its power is greater than each other creature's power" — a relative-maximum-power
/// superlative gate. Selvala, Heart of the Wilds's ETB draw condition: the entering
/// creature's controller draws only if that creature's power is strictly the greatest on
/// the battlefield. The predicate spans the WHOLE set of other creatures ("greater than
/// EACH other creature's power"), which no per-object <see cref="ObjectFilter"/> comparison
/// axis can express — a <see cref="ObjectFilter.PowerComparison"/> relative to a single
/// referent compares against one object, not the maximum over a population.
///
/// <para>
/// <see cref="Subject"/> is the creature whose power is asserted greatest (Selvala's "it" =
/// the entering creature, <see cref="ObjectReferenceKind.It"/>); <see cref="Among"/> is the
/// population it must exceed (all creatures — the subject is trivially excluded, since a
/// thing is not greater than itself); <see cref="IncludeTies"/> is <c>false</c> for the
/// strict "greater than each" form (ties fail the gate) and would be <c>true</c> for a
/// hypothetical "greatest or tied" sibling. The sibling superlative of
/// <see cref="MostCommonColorCondition"/> (a most-common tally) and
/// <see cref="PlayerHasMostLifeCondition"/> (a most-life tally): a card-defined,
/// engine-evaluated maximum recorded as written.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed superlative; the engine
/// tallies the powers and compares, MAST does not pre-evaluate it. Structured rather than
/// left as a free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 205.3 / 208 (power); there is no CR rule for "greatest power" — it is a card-defined,
/// engine-evaluated maximum.
/// </summary>
[ConditionKind("greatestPower")]
public sealed record GreatestPowerCondition : Condition
{
  /// <summary>The object whose power is asserted greatest — Selvala's entering creature is <see cref="ObjectReferenceKind.It"/>.</summary>
  public required ObjectReference Subject { get; init; }

  /// <summary>The population the subject's power must exceed — Selvala's is all creatures (<c>{CardTypes:["creature"]}</c>).</summary>
  public required ObjectFilter Among { get; init; }

  /// <summary>
  /// <c>false</c> for the strict "greater than each" form (a tie for greatest fails the
  /// gate — Selvala); <c>true</c> for a "greatest or tied for greatest" form.
  /// </summary>
  public required bool IncludeTies { get; init; }
}
