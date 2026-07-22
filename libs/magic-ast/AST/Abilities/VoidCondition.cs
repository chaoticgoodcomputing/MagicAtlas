namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "a nonland permanent left the battlefield this turn or a spell was warped this
/// turn" — the fixed backward-looking, turn-scoped event-history gate the Edge of
/// Eternities <em>Void</em> ability word denotes. A disjunction of two independent
/// this-turn events: (1) a nonland permanent left the battlefield this turn (a
/// leaves-the-battlefield event, CR 603.6c / 603.10a, restricted to nonland
/// permanents), OR (2) a spell was warped this turn (CR 702.185c: "a spell was
/// warped this turn" means a spell was cast for its warp cost this turn). The gate
/// holds when EITHER disjunct is satisfied.
///
/// <para>
/// A field-less marker: "Void" is an ability word with no rules meaning (CR 207.2c
/// lists "void" among the ability words — ability words "tie together cards that
/// have similar functionality, but they have no special rules meaning and no
/// individual entries in the Comprehensive Rules"), so the printed disjunction —
/// identical, verbatim, on every Void card — <em>is</em> the condition, not a
/// keyword whose definition varies by parameter. Neither disjunct nor the
/// disjunction structure ever varies across the family, so there is nothing to
/// parameterise; the node's identity encodes the fixed two-event disjunction.
/// Mirrors the field-less-idiom convention already used for fixed condition idioms
/// (<see cref="CastThisObjectCondition"/>, <see cref="PrecedingActionPerformedCondition"/>).
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed event-history gate;
/// the engine reads the actual this-turn game history (whether a nonland permanent
/// left the battlefield, whether a spell was warped) and evaluates the disjunction,
/// MAST does not pre-evaluate it. Structured to this dedicated
/// <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 207.2c (excerpt): "An ability word appears in italics at the beginning of some
/// abilities. Ability words … have no special rules meaning and no individual
/// entries in the Comprehensive Rules. The ability words are … void …."
/// CR 702.185c (verbatim): "Some effects refer to whether 'a spell was warped this
/// turn.' This means that a spell was cast for its warp cost this turn."
/// CR 603.10a (excerpt): leaves-the-battlefield abilities "look back in time".
/// </summary>
[ConditionKind("void")]
public sealed record VoidCondition : Condition;
