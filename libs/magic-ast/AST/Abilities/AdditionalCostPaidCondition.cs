namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if you paid life this way" — true when the OPTIONAL prose additional cost
/// granted earlier in the SAME static ability
/// (<see cref="MagicAST.AST.Effects.Resource.GrantAdditionalCostEffect"/>) was
/// paid as the affected spell was cast. The non-keyword sibling of
/// <see cref="KeywordCostPaidCondition"/>: that node keys on a
/// <see cref="KeywordAbility"/> identity (Kicker, Evoke, Dash, Blitz); this one
/// has no keyword to key on because the additional cost is prose, not a
/// CR-defined keyword ability. A marker (no fields) — like
/// <see cref="PrecedingActionPerformedCondition"/>, "this way" always refers to
/// the single additional-cost grant printed earlier on the same ability, so
/// there is nothing to parameterise.
///
/// <para>
/// CR 601.2f (verbatim): "The player determines the total cost of the spell.
/// Usually this is just the mana cost. Some spells have additional or
/// alternative costs. ... The total cost is the mana cost or alternative cost
/// (as determined in rule 601.2b), plus all additional costs and cost
/// increases, and minus all cost reductions." This condition gates the "minus
/// all cost reductions" half on whether the "plus all additional costs" half
/// was actually paid (the additional cost is optional).
/// </para>
///
/// <para>
/// Canonical use: Defiler of Instinct — "As an additional cost to cast red
/// permanent spells, you may pay 2 life. Those spells cost {R} less to cast
/// if you paid life this way." Reference-not-resolution (ADR 0004): the engine
/// tracks whether the optional cost was paid; MAST records the reference to
/// the same ability's own additional-cost grant, not a pre-resolved boolean.
/// </para>
/// </summary>
[ConditionKind("additionalCostPaid")]
public sealed record AdditionalCostPaidCondition : Condition;
