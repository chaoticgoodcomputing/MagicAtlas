namespace MagicAST.AST;

using System.Text.Json.Serialization;

/// <summary>
/// Represents parenthetical content in oracle text, typically reminder text.
/// Rule 207.2: Reminder text is italicized text in parentheses that summarizes a rule.
/// Parentheticals have no rules meaning but provide clarification.
/// </summary>
public sealed record Parenthetical
{
  /// <summary>
  /// The text content inside the parentheses, without the parentheses themselves.
  /// </summary>
  [JsonPropertyName("text")]
  public required string Text { get; init; }
}
