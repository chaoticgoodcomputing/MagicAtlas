using Flowthru.Data.Schema;
using MagicAST;

namespace MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;

/// <summary>
/// Per-card parse summary produced by the triage flow's parse step. Captures
/// enough per-line outcome detail for the aggregation step to compute pattern
/// frequencies, cleanliness scores, and projected coverage gains without
/// re-parsing.
/// </summary>
[FlowthruSchema]
public partial record ParseRecord
{
  /// <summary>Scryfall card id — joins back to the source row.</summary>
  public required string ScryfallId { get; init; }

  /// <summary>Card name (denormalised for fast report rendering).</summary>
  public required string CardName { get; init; }

  /// <summary>The DTO that was fed to the parser (echoed for downstream display).</summary>
  public required CardInputDTO Input { get; init; }

  /// <summary>Per-line outcomes, in oracle-text order. Empty if the card has no oracle text.</summary>
  public required IReadOnlyList<LineOutcome> Lines { get; init; }
}

/// <summary>
/// A single newline-bounded oracle line and the diagnostic patterns it produced
/// when parsed independently. A line is considered "passing" when
/// <see cref="Patterns"/> is empty (the parser produced a non-<c>UnparsedAbility</c>
/// for every clause in the line).
/// </summary>
[FlowthruSchema]
public partial record LineOutcome
{
  /// <summary>Zero-based index of this line in the original oracle text.</summary>
  public required int LineIndex { get; init; }

  /// <summary>The raw oracle line text (the chunk between newlines).</summary>
  public required string OracleLine { get; init; }

  /// <summary>
  /// One entry per diagnostic emitted while parsing this line. Each entry is the
  /// <c>Diagnostic.Pattern</c> string from the corresponding
  /// <c>UnparsedAbility</c>. The list may contain duplicates when a line splits
  /// into multiple unparsed clauses sharing the same pattern.
  /// </summary>
  public required IReadOnlyList<string> Patterns { get; init; }
}
