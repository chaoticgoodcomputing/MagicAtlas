namespace MagicAST.AST;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a range of characters in the original oracle text.
/// Used for error reporting and round-tripping.
/// </summary>
public readonly record struct TextSpan(int Start, int Length)
{
    public int End => Start + Length;

    public static TextSpan Empty => new(0, 0);

    public static TextSpan FromBounds(int start, int end) => new(start, end - start);
}
