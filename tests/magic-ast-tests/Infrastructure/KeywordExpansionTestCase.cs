namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;

/// <summary>
/// One keyword-expansion gold case, loaded from a JSON file in
/// <c>Data/KeywordExpansions</c>. Unlike <see cref="CardTestCase"/> there is no
/// example card: the spec is the keyword's own canonical printed form
/// (<see cref="Keyword"/> + optional <see cref="Parameter"/>), and the expected
/// output is the decomposed <c>Ability</c> subtree that the keyword's combinator
/// must emit. See ADR 0003 (keywords decompose into shared primitives).
/// </summary>
public sealed class KeywordExpansionTestCase
{
  /// <summary>Test name, derived from the gold's filename (usually the keyword).</summary>
  public required string Name { get; init; }

  /// <summary>Path to the source JSON file.</summary>
  public required string FilePath { get; init; }

  /// <summary>
  /// The keyword word as it appears in printed text (e.g. "Bushido", "Flashback",
  /// "First strike").
  /// </summary>
  public required string Keyword { get; init; }

  /// <summary>
  /// The printed parameter that follows the keyword, verbatim, or null for
  /// parameterless keywords. E.g. "2" (Bushido 2), "{3}{R}" (Flashback {3}{R}),
  /// "from red" (Protection from red), "for artifacts" (Affinity for artifacts).
  /// </summary>
  public string? Parameter { get; init; }

  /// <summary>The expected decomposed <c>Ability</c> subtree (raw JSON node).</summary>
  public required JsonNode ExpectedNode { get; init; }

  /// <summary>
  /// The canonical printed form fed to the tokenizer + keyword combinator:
  /// <c>Keyword</c>, then a space and <c>Parameter</c> when present.
  /// </summary>
  public string PrintedForm =>
    string.IsNullOrWhiteSpace(Parameter) ? Keyword : $"{Keyword} {Parameter}";

  public override string ToString() => Name;
}
