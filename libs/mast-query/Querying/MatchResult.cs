namespace MagicAST.Query;

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
  public IReadOnlyDictionary<string, string>? Captures { get; init; }
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
