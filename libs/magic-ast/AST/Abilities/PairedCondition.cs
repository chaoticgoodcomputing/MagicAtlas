namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[this creature] is paired with another creature" — the Soulbond pairing gate
/// (CR 702.95). True while the source permanent is currently paired with another
/// creature via the soulbond keyword: "You may pair this creature with another
/// unpaired creature when either enters. They remain paired for as long as you
/// control both of them." (CR 702.95a). The "as long as …" grants a soulbond card
/// prints (Deadeye Navigator's activated ability, Tandem Lookout's damage trigger,
/// Wingcrafter's flying) apply exactly while this gate holds.
///
/// <para>
/// A field-less marker: the subject is always the source permanent — soulbond is a
/// self-referential keyword, so "X is paired with another creature" names the same
/// object as the card it prints on (the printed subject is the card's own name, e.g.
/// "Deadeye Navigator is paired …", or the bare "this creature is paired …"). The
/// referent never varies (it is always <c>Self</c>) and the phrasing "with another
/// creature" is fixed across the family, so there is nothing to parameterise. Mirrors
/// the field-less-idiom convention used for other fixed condition idioms
/// (<see cref="VoidCondition"/>, <see cref="ControlSinceLastUpkeepCondition"/>,
/// <see cref="PrecedingActionPerformedCondition"/>). Structured to this dedicated
/// <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed pairing gate; the
/// engine reads whether the source is actually paired (whether a soulbond pairing was
/// formed on entry and both creatures are still controlled by the same player, CR
/// 702.95a), MAST does not pre-evaluate it.
/// </para>
///
/// CR 702.95a (excerpt): "Soulbond is a keyword that represents two abilities. … 'You
/// may pair this creature with another unpaired creature when either enters. They
/// remain paired for as long as you control both of them.'"
/// </summary>
[ConditionKind("paired")]
public sealed record PairedCondition : Condition;
