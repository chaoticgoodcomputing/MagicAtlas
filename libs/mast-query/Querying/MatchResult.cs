namespace MagicAST.Query;

using System.Text.Json.Nodes;

/// <summary>
/// Three-valued match outcome (mast-query ADR-0001), the certain-answers lattice:
/// a certain match, a certain non-match, or possible-but-not-certain — the last arising when an
/// unparsed region falls within the pattern's scope, so "no" cannot be asserted honestly.
/// </summary>
public enum Determinacy
{
  Match,
  NoMatch,
  Unknown,
}

/// <summary>Trust tier of a match (mast-query ADR-0001): a structural match outranks one rescued
/// from unparsed text by a <c>$unparsed-regex</c> fallback.</summary>
public enum Provenance
{
  Structural,
  UnparsedRegex,
}

/// <summary>One card's outcome under a query: its determinacy, where it matched, any captured
/// bindings, and — for an <see cref="Determinacy.Unknown"/> — why it could not be decided.</summary>
public sealed record CardMatch
{
  public required string Card { get; init; }
  public Determinacy Determinacy { get; init; }
  public Provenance Provenance { get; init; } = Provenance.Structural;
  public string? Path { get; init; }

  /// <summary>Captured nodes by name — the matched <c>JsonNode</c> (detached clone), so a consumer
  /// can deserialize it to a typed node (e.g. an <c>ObjectFilter</c>) and feed it to the
  /// interaction join. Was stringified; retained as a node so the cross-query join layer has the
  /// real subtree, not its canonical text.</summary>
  public IReadOnlyDictionary<string, JsonNode>? Captures { get; init; }
  public string? Reason { get; init; }
}

/// <summary>The result of running one query over a corpus, partitioned into the certain matches,
/// the indeterminate cards, and a count of the certain non-matches.</summary>
public sealed record QueryResult
{
  public required string Query { get; init; }
  public required IReadOnlyList<CardMatch> Matched { get; init; }
  public required IReadOnlyList<CardMatch> Unknown { get; init; }
  public int NonMatch { get; init; }
}

/// <summary>
/// Outcome of matching a pattern at a single node (subtree-rooted) rather than over a card corpus —
/// the entry point the interaction engine uses to match family patterns at <em>ports</em> (ability
/// sub-trees). Carries the three-valued <see cref="Determinacy"/>, where it matched, and the typed
/// captures the cross-query join consumes.
/// </summary>
public sealed record MatchOutcome(
  Determinacy Determinacy,
  string? Path,
  IReadOnlyDictionary<string, JsonNode>? Captures
);
