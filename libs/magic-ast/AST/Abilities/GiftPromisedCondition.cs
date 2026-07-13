namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if the gift was promised" / "if this spell's gift cost was paid" — true when the
/// controller of a spell with the Gift keyword (CR 702.174) declared the intention to
/// pay the spell's gift cost as it was cast. CR 702.174k (verbatim): "If a spell's
/// controller declares the intention to pay a spell's gift cost, that spell's gift was
/// promised." CR 702.174b gates the gift's own second ability on the same fact ("If this
/// spell's gift cost was paid, [effect]"), and cards such as Wildfire Howl gate a
/// distinct spell effect on it too ("If the gift was promised, instead …").
///
/// <para>A marker (no fields), like <see cref="AdditionalCostPaidCondition"/>: "the
/// gift" always refers to the single gift cost of the spell carrying the condition, so
/// there is nothing to parameterise. Reference-not-resolution (ADR 0004): the engine
/// tracks whether the gift was promised; MAST records only the reference to that fact,
/// not a pre-resolved boolean.</para>
/// </summary>
[ConditionKind("giftPromised")]
public sealed record GiftPromisedCondition : Condition;
