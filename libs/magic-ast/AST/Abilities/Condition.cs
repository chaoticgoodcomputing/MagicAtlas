namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A predicate over game state — "you control a Forest", "seven or more cards in
/// your graveyard", "enchanted creature is black". Modelled as one discriminated
/// union that <i>composes the existing primitives</i> (<see cref="ObjectFilter"/>,
/// <see cref="Comparison"/>, and — in later arms — <c>HistoryPredicate</c>)
/// rather than introducing a new one. The single home for every "if …",
/// "as long as …", and "unless …"-style predicate, replacing the former stringly
/// <c>Condition { Text }</c> record and the bare <c>string Condition</c> fields.
///
/// <para>
/// Encoded as written; the engine evaluates it against game state — a
/// <see cref="CountCondition"/> is "you control a Forest", never a pre-resolved
/// boolean (ADR 0004, reference-not-resolution). Seeded worst-first (ADR 0001):
/// <see cref="CountCondition"/> for the dominant shape and the
/// <see cref="OtherCondition"/> residual for the rest; history, object-state, and
/// compound arms are added as the card families that need them land. See ADR 0007.
/// </para>
/// </summary>
[PolymorphicBase("ConditionType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Condition>))]
public abstract record Condition
{
  /// <summary>The typed residual for a not-yet-structured condition phrase.</summary>
  public static OtherCondition Other(string text) => new() { Text = text };
}

/// <summary>
/// A count of objects matching a filter, compared to a threshold — the dominant
/// condition shape ("you control two or more artifacts", "seven or more cards in
/// your graveyard", "two or fewer other lands"). Composes <see cref="ObjectFilter"/>
/// (which objects — controller, zone, types) and <see cref="Comparison"/>
/// (the threshold).
/// </summary>
[ConditionKind("count")]
public sealed record CountCondition : Condition
{
  /// <summary>Which objects are counted — controller, zone, types, etc.</summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>The threshold the count is compared against.</summary>
  public required Comparison Count { get; init; }
}

/// <summary>
/// "if its [keyword] cost was paid" — true when the additional/alternative cost a keyword
/// grants was paid as the spell was cast: Kicker/Multikicker ("if it was kicked", CR
/// 702.33d; a multikicker cost is a kicker cost, 702.33c), Evoke ("if its evoke cost was
/// paid", 702.74a), Dash (702.109a), Blitz (702.152a). The consumer half of the keyword
/// production/reference duality (ADR 0003/0004): reference-not-resolution — keyed on the
/// producing keyword's typed <see cref="KeywordAbility"/> identity (a linked ability, e.g.
/// CR 702.33e), NOT a pre-resolved boolean threaded from the cost ability. The matching
/// producer (<see cref="StaticAbility.KeywordSource"/> + its <c>AlternativeCastEffect</c> /
/// <c>AdditionalCastCostEffect</c>) rides on the same card. Count sibling:
/// <see cref="MagicAST.AST.Quantities.KeywordCostPaidCountQuantity"/>.
/// </summary>
[ConditionKind("keywordCostPaid")]
public sealed record KeywordCostPaidCondition : Condition
{
  /// <summary>
  /// The keyword whose additional/alternative cost being paid makes this true — Kicker
  /// (a multikicker cost is a kicker cost, CR 702.33c, so "kicked" keys on Kicker), Evoke,
  /// Dash, Blitz, etc.
  /// </summary>
  public required KeywordAbility Keyword { get; init; }
}

/// <summary>
/// "if it had a +1/+1 counter on it" / "if it had no +1/+1 counters on it" — the triggering object
/// (the dying/affected permanent a leaves-the-battlefield trigger refers to) had, or had not, a counter
/// of the given kind immediately before the event (CR 603.10 — dies-triggers look back in time). The
/// structured form of the recurring Persist / Undying / Basri's-Lieutenant counter-gate, replacing the
/// free-text residual. Reference-not-resolution: the engine reads the dying object's last-known counters,
/// not a pre-evaluated boolean.
/// </summary>
[ConditionKind("triggeringObjectCounter")]
public sealed record TriggeringObjectCounterCondition : Condition
{
  /// <summary>The counter kind the condition checks (e.g. <c>"+1/+1"</c>, <c>"-1/-1"</c>).</summary>
  public required string CounterType { get; init; }

  /// <summary>True for "had a [counter]" (≥1 present); false for "had no [counters]" (0 present).</summary>
  public required bool Present { get; init; }
}

/// <summary>
/// "if X is greater than or equal to [quantity]" — a condition that compares two
/// <see cref="Quantity"/> values. Covers the Thassa's Oracle win-condition shape:
/// "If X is greater than or equal to the number of cards in your library, you win
/// the game." (where X is the devotion-derived look count).
///
/// <para>
/// CR 700.5 (devotion); MAST records the comparison as written; the engine resolves
/// both operands against game state (ADR 0004: reference-not-resolution). Neither
/// operand is pre-evaluated.
/// </para>
/// </summary>
[ConditionKind("quantityComparison")]
public sealed record QuantityComparisonCondition : Condition
{
  /// <summary>The left-hand quantity operand (e.g., the variable X = devotion to blue).</summary>
  public required Quantity Left { get; init; }

  /// <summary>The comparison operator.</summary>
  public required ComparisonOperator Operator { get; init; }

  /// <summary>The right-hand quantity operand (e.g., cards in your library).</summary>
  public required Quantity Right { get; init; }
}

/// <summary>
/// "if it isn't a mana ability" / "if it's a mana ability" — an intervening-if (CR 603.4) on an
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.AbilityActivated"/> trigger that gates on whether the
/// triggering ability is a mana ability (CR 605.1: a mana ability is an activated/triggered ability that
/// could add mana, doesn't target, and isn't a loyalty ability). The structured form of Rings of
/// Brighthearth's "if it isn't a mana ability" — NOT a free-text <see cref="OtherCondition"/> residual.
/// <see cref="IsManaAbility"/> carries the polarity: <c>false</c> = "isn't a mana ability".
/// </summary>
[ConditionKind("triggeringAbilityIsMana")]
public sealed record TriggeringAbilityIsManaCondition : Condition
{
  /// <summary>Whether the condition requires the triggering ability TO BE a mana ability. <c>false</c>
  /// encodes the "isn't a mana ability" form (Rings of Brighthearth); <c>true</c> the affirmative.</summary>
  public required bool IsManaAbility { get; init; }
}

/// <summary>
/// "if you cast it" — an intervening-if (CR 603.4) on a self ETB trigger that gates on whether the
/// source object entered the battlefield by being CAST (CR 601), as opposed to being created by a
/// copy effect, reanimated, blinked, or otherwise put onto the battlefield (CR 707.10 — a copy isn't
/// cast). The structured form of The One Ring's "if you cast it" — NOT a free-text
/// <see cref="OtherCondition"/> residual. A marker (no fields): the affirmative "you cast it" is the
/// only form; a copy/reanimate entry fails this gate so the consequent (the protection) does not apply.
/// </summary>
[ConditionKind("castThisObject")]
public sealed record CastThisObjectCondition : Condition;

/// <summary>
/// "if it's a Unicorn" / "if it's an Elf" — a condition that checks whether a designated
/// game object (the "it" pronoun from the enclosing ability context, typically the creature
/// that entered the battlefield in an ETB trigger) has a specific creature subtype.
///
/// <para>
/// CR 205.3m (Creature subtypes): creature subtypes are listed after the type-line dash and
/// are checked at resolution by looking at the object's current characteristic. MAST records
/// the condition as written — reference-not-resolution (ADR 0004): the engine reads the
/// object's type line; MAST does not pre-evaluate it.
/// </para>
///
/// <para>
/// The <see cref="Subject"/> field disambiguates the pronoun ("It" for the standard "if it's
/// a [Subtype]" oracle form, the most common case). "It" refers to the same object as the
/// preceding effect's target — in Emiel the Blessed, the creature that just entered the
/// battlefield and received the counter.
/// </para>
///
/// CR 205.3m (verbatim): "Creature subtypes are always a single word and are listed after a
/// long dash on the card's type line. ..."
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's trigger
/// event, that ability automatically triggers."
/// </summary>
[ConditionKind("objectHasSubtype")]
public sealed record ObjectHasSubtypeCondition : Condition
{
  /// <summary>
  /// The creature subtype to check — e.g. <c>"Unicorn"</c>, <c>"Elf"</c>.
  /// Always the proper-cased single word as it appears on type lines (CR 205.3m).
  /// </summary>
  public required string Subtype { get; init; }

  /// <summary>
  /// The pronoun that identifies the subject object — typically <c>"It"</c>
  /// (the object referred to by the containing ability context, e.g. the entering
  /// creature in an ETB trigger). Uses title-case to match the ObjectReferenceKind
  /// vocabulary.
  /// </summary>
  public required string Subject { get; init; }
}

/// <summary>
/// Typed residual for a condition that does not yet have a structured variant —
/// carries the literal oracle phrase. A deferral, not a destination (ADR 0001):
/// counted by the residual-debt metric, structured when the shape recurs.
/// </summary>
[ConditionKind("other")]
public sealed record OtherCondition : Condition, IResidual
{
  /// <summary>The literal condition phrase from the oracle text.</summary>
  public required string Text { get; init; }
}
