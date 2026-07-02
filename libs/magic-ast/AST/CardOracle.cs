namespace MagicAST.AST;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;

/// <summary>
/// Root node of the parsed AST representing all abilities on a card.
/// </summary>
public sealed record CardOracle
{
  /// <summary>
  /// The original oracle text that was parsed.
  /// </summary>
  public required string RawText { get; init; }

  /// <summary>
  /// The parsed abilities from the oracle text.
  /// Each paragraph in oracle text typically represents one ability.
  /// </summary>
  public required IReadOnlyList<Ability> Abilities { get; init; }
}
