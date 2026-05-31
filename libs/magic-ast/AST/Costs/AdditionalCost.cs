namespace MagicAST.AST.Costs;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Represents an additional cost defined in oracle text.
/// "As an additional cost to cast this spell, [cost]"
/// </summary>
public sealed record AdditionalCost
{
    /// <summary>
    /// The cost that must be paid.
    /// </summary>
    public required Cost Cost { get; init; }

    /// <summary>
    /// Whether this additional cost is optional.
    /// e.g., "you may sacrifice a creature"
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// "any number of times" — the additional cost may be paid repeatedly
    /// (Multikicker, Squad, Replicate, Escalate). False when it may be paid at most
    /// once (Kicker, Buyback, Entwine) or exactly once (a mandatory prose cost).
    /// </summary>
    public bool Repeatable { get; init; }

    /// <summary>
    /// Alternative to the additional cost, if any.
    /// e.g., "reveal a Dinosaur card from your hand or pay {1}"
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Cost? Alternative { get; init; }

    /// <summary>
    /// Location in source text — the frontier span for a cost parsed from prose
    /// ("As an additional cost to cast this spell, ..."). Omitted (null) on a cost
    /// synthesized from a keyword expansion (Kicker/Multikicker/Entwine/Escalate),
    /// whose identity rides on the enclosing ability's <c>KeywordSource</c> instead.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextSpan? SourceSpan { get; init; }
}

/// <summary>
/// Represents an alternative cost that can be paid instead of the mana cost.
/// </summary>
public sealed record AlternativeCost
{
    /// <summary>
    /// The cost that can be paid instead.
    /// </summary>
    public required Cost Cost { get; init; }

    /// <summary>
    /// Condition that must be met to use this alternative cost.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Condition? Condition { get; init; }

    /// <summary>
    /// Location in source text.
    /// </summary>
    public required TextSpan SourceSpan { get; init; }
}

/// <summary>
/// Represents a cost reduction effect.
/// e.g., "This spell costs {1} less to cast for each creature you control"
/// </summary>
public sealed record CostReduction
{
    /// <summary>
    /// The amount of the reduction.
    /// </summary>
    public required Quantity Amount { get; init; }

    /// <summary>
    /// What the reduction is per.
    /// e.g., "for each creature you control"
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ObjectFilter? Per { get; init; }

    /// <summary>
    /// Condition for the reduction.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Condition? Condition { get; init; }

    /// <summary>
    /// Location in source text.
    /// </summary>
    public required TextSpan SourceSpan { get; init; }
}
