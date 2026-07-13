namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if the [cost] cost was paid" — true when the card's own printed alternative
/// cost (CR 118.9, e.g. "You may pay {3}{G} rather than pay this spell's mana
/// cost.") was the cost actually paid to cast the spell. The non-keyword sibling
/// of <see cref="KeywordCostPaidCondition"/> (which keys on a CR-defined keyword
/// identity — Kicker, Evoke, Dash, Blitz) and of
/// <see cref="AdditionalCostPaidCondition"/> (which keys on an OPTIONAL additional
/// cost granted earlier in the same static ability): this one has no keyword to
/// key on, and it references a REPLACEMENT cost (an alternative, not an addition)
/// printed as its own ability elsewhere on the same card — typically the sibling
/// <see cref="MagicAST.AST.Effects.Resource.GrantAlternativeCostEffect"/> ability
/// (self-scoped, <c>AffectedSpells.IsSelf = true</c>). A marker (no fields): the
/// oracle always refers back to "the [same literal cost]", so there is nothing to
/// parameterise beyond the reference itself.
///
/// <para>
/// CR 601.2f (verbatim): "The player determines the total cost of the spell.
/// Usually this is just the mana cost. Some spells have additional or
/// alternative costs. ... The total cost is the mana cost or alternative cost
/// (as determined in rule 601.2b), plus all additional costs and cost
/// increases, and minus all cost reductions." This condition gates a downstream
/// effect on whether the alternative-cost branch of 601.2b was the one taken.
/// </para>
///
/// <para>
/// Canonical use: Verdant Mastery — "You may pay {3}{G} rather than pay this
/// spell's mana cost. ... Put one of them onto the battlefield tapped under an
/// opponent's control if the {3}{G} cost was paid. ..." Reference-not-resolution
/// (ADR 0004): the engine tracks which cost was actually paid; MAST records the
/// reference to the card's own alternative-cost grant, not a pre-resolved boolean.
/// </para>
/// </summary>
[ConditionKind("alternativeCostPaid")]
public sealed record AlternativeCostPaidCondition : Condition;
