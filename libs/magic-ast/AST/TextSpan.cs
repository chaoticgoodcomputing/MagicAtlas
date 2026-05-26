namespace MagicAST.AST;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a range of characters in the original oracle text.
/// Used for error reporting and round-tripping.
/// </summary>
public readonly record struct TextSpan(int Start, int Length)
{
    /// <summary>
    /// Computed end offset (exclusive). Derived from <see cref="Start"/> and <see cref="Length"/> —
    /// must not round-trip through JSON, otherwise it would appear as a redundant property
    /// in every fixture using a TextSpan.
    /// </summary>
    [JsonIgnore]
    public int End => Start + Length;

    public static TextSpan Empty => new(0, 0);

    public static TextSpan FromBounds(int start, int end) => new(start, end - start);
}
