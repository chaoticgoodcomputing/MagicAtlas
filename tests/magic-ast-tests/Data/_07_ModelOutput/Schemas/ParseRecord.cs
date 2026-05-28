using Flowthru.Data.Schema;
using MagicAST;

namespace MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;

/// <summary>
/// Per-card parse summary produced by the triage flow's parse step. The parser
/// is run ONCE over the whole card's oracle text (so multi-line constructs like
/// modal, saga, and level-up are grouped by <c>ClauseSplitter</c> as the real
/// parser sees them); per-line outcomes are then derived by attributing each
/// <c>UnparsedAbility</c> back to the oracle line(s) its <c>SourceSpan</c> covers.
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

  /// <summary>
  /// Total abilities the parser produced for the whole card, from the single
  /// full-card parse. Authoritative source for the corpus-wide AbilityCoverage
  /// metric — each ability is counted once even when it spans multiple oracle
  /// lines (modal, saga, level-up), which a per-line sum would double-count.
  /// </summary>
  public required int TotalAbilities { get; init; }

  /// <summary>Card-level count of abilities that were NOT <c>UnparsedAbility</c>.</summary>
  public required int ParsedAbilities { get; init; }

  /// <summary>Per-line outcomes, in oracle-text order. Empty if the card has no oracle text.</summary>
  public required IReadOnlyList<LineOutcome> Lines { get; init; }
}

/// <summary>
/// A single newline-bounded oracle line and the diagnostics attributed to it
/// from the full-card parse. A line is considered "passing" when
/// <see cref="Patterns"/> is empty — i.e. no <c>UnparsedAbility</c>'s
/// <c>SourceSpan</c> overlaps this line's character range. A multi-line
/// construct that parses (e.g. a modal "Choose one —" header plus its bullet
/// options) leaves every one of its lines passing.
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

  /// <summary>
  /// One entry per diagnostic emitted while parsing this line, in lockstep with
  /// <see cref="Patterns"/>. Each entry bundles the pattern with the parser-rule
  /// near-miss telemetry (<c>LastAttemptedRule</c> + <c>FailurePosition</c>)
  /// from the originating <c>Diagnostic</c>. The aggregator uses the bundled
  /// shape to cluster failures by <c>(pattern, lastAttemptedRule)</c> instead
  /// of by <c>pattern</c> alone.
  /// </summary>
  public required IReadOnlyList<LineDiagnostic> Diagnostics { get; init; }
}

/// <summary>
/// Per-diagnostic detail extracted from one <c>UnparsedAbility</c> emitted by
/// the parser for a single oracle line. Mirrors the relevant fields of
/// <c>MagicAST.Diagnostics.Diagnostic</c> needed by the triage aggregator.
/// </summary>
[FlowthruSchema]
public partial record LineDiagnostic
{
  /// <summary>The <c>Diagnostic.Pattern</c> string (e.g. "ConditionalEffect").</summary>
  public required string Pattern { get; init; }

  /// <summary>
  /// Name of the parser rule that came closest to matching before fallback —
  /// e.g. <c>"SpellAbilityParser.Parse"</c>. May be null on legacy diagnostics
  /// that pre-date the telemetry wiring.
  /// </summary>
  public string? LastAttemptedRule { get; init; }

  /// <summary>
  /// Character offset within the source oracle line at which the parser bailed.
  /// </summary>
  public int? FailurePosition { get; init; }
}
