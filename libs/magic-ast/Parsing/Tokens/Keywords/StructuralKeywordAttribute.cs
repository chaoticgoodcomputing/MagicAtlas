namespace MagicAST.Parsing.Tokens.Keywords;

/// <summary>
/// Marks a class as the registration site for a structural keyword recognized by
/// <see cref="OracleTokenizer"/>. The class body is conventionally empty — the
/// attribute is the entire registration. Each tagged class is discovered at runtime
/// by <see cref="StructuralKeywordRegistry"/>.
/// </summary>
/// <param name="text">The keyword text (case-insensitive match against tokenized words).</param>
/// <param name="token">The <see cref="OracleToken"/> emitted when the word matches.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StructuralKeywordAttribute(string text, OracleToken token) : Attribute
{
  /// <summary>The keyword text. Matched case-insensitively.</summary>
  public string Text { get; } = text;

  /// <summary>The token emitted on match.</summary>
  public OracleToken Token { get; } = token;
}
