namespace MagicAST.AST.References;

/// <summary>
/// Three-valued result of <see cref="ObjectFilterRelations.Intersects"/>: can two
/// <see cref="ObjectFilter"/>s denote a common game object? <see cref="Unknown"/> is the soundness
/// floor — an axis the operator does not yet decide yields it rather than a false yes/no (magic-ast
/// ADR-0008 / the operator spec). Composes upward into mast-int as a candidate-pending edge, the
/// same tri-state discipline as the parser's <c>IUnparsed</c>.
/// </summary>
public enum FilterRelation
{
  /// <summary>The two filters can denote a common object.</summary>
  Overlaps,

  /// <summary>Provably no object satisfies both — one axis contradicts.</summary>
  Disjoint,

  /// <summary>Undecidable on the implemented axes — never a false <see cref="Overlaps"/> or <see cref="Disjoint"/>.</summary>
  Unknown,
}

/// <summary>
/// An <see cref="ObjectFilterRelations.Intersects"/> verdict with optional provenance: the axis
/// that decided it — the contradicting axis for <see cref="FilterRelation.Disjoint"/>, the first
/// undecided axis for <see cref="FilterRelation.Unknown"/>, <c>null</c> for
/// <see cref="FilterRelation.Overlaps"/>. Mirrors mast-query's <c>CardMatch.Reason</c>: every
/// Unknown names what to implement (or parse) next, so the operator's holes double as a
/// coverage-pressure signal.
/// </summary>
public readonly record struct FilterMatch(FilterRelation Relation, string? Reason = null);
