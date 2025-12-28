namespace MagicAST.AST.Costs;

using System.Text.Json.Serialization;
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
    [JsonPropertyName("cost")]
    public required Cost Cost { get; init; }

    /// <summary>
    /// Whether this additional cost is optional.
    /// e.g., "you may sacrifice a creature"
    /// </summary>
    [JsonPropertyName("isOptional")]
    public bool IsOptional { get; init; }

    /// <summary>
    /// Alternative to the additional cost, if any.
    /// e.g., "reveal a Dinosaur card from your hand or pay {1}"
    /// </summary>
    [JsonPropertyName("alternative")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Cost? Alternative { get; init; }

    /// <summary>
    /// Location in source text.
    /// </summary>
    [JsonPropertyName("sourceSpan")]
    public required TextSpan SourceSpan { get; init; }
}

/// <summary>
/// Represents an alternative cost that can be paid instead of the mana cost.
/// </summary>
public sealed record AlternativeCost
{
    /// <summary>
    /// The cost that can be paid instead.
    /// </summary>
    [JsonPropertyName("cost")]
    public required Cost Cost { get; init; }

    /// <summary>
    /// Condition that must be met to use this alternative cost.
    /// </summary>
    [JsonPropertyName("condition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Condition { get; init; }

    /// <summary>
    /// Location in source text.
    /// </summary>
    [JsonPropertyName("sourceSpan")]
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
    [JsonPropertyName("amount")]
    public required Quantity Amount { get; init; }

    /// <summary>
    /// What the reduction is per.
    /// e.g., "for each creature you control"
    /// </summary>
    [JsonPropertyName("per")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ObjectFilter? Per { get; init; }

    /// <summary>
    /// Condition for the reduction.
    /// </summary>
    [JsonPropertyName("condition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Condition { get; init; }

    /// <summary>
    /// Location in source text.
    /// </summary>
    [JsonPropertyName("sourceSpan")]
    public required TextSpan SourceSpan { get; init; }
}
