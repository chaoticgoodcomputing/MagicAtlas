namespace MagicAST.AST.References;

/// <summary>
/// The canonical three-valued (Kleene) logic value for MAST — MAST being the most-upstream library
/// (magic-ast ADR-0008), this is the primitive downstream three-valued models map onto rather than
/// each re-inventing one. <see cref="ObjectFilterRelations.Subsumes"/> returns it directly;
/// <c>FilterRelation</c> is a sibling domain-named enum over the same lattice (<c>Overlaps≈Yes</c>,
/// <c>Disjoint≈No</c>), kept distinct so call sites read in their own vocabulary.
/// </summary>
public enum Trilean
{
  /// <summary>Provably true.</summary>
  Yes,

  /// <summary>Provably false.</summary>
  No,

  /// <summary>Undecidable on the available information — never a false <see cref="Yes"/> or <see cref="No"/>.</summary>
  Unknown,
}

/// <summary>Kleene (strong) three-valued connectives over <see cref="Trilean"/>.</summary>
public static class Kleene
{
  /// <summary>Conjunction: <see cref="Trilean.No"/> absorbs; <see cref="Trilean.Unknown"/> dominates <see cref="Trilean.Yes"/>.</summary>
  public static Trilean And(Trilean a, Trilean b) =>
    a == Trilean.No || b == Trilean.No ? Trilean.No
    : a == Trilean.Unknown || b == Trilean.Unknown ? Trilean.Unknown
    : Trilean.Yes;

  /// <summary>Disjunction: <see cref="Trilean.Yes"/> absorbs; <see cref="Trilean.Unknown"/> dominates <see cref="Trilean.No"/>.</summary>
  public static Trilean Or(Trilean a, Trilean b) =>
    a == Trilean.Yes || b == Trilean.Yes ? Trilean.Yes
    : a == Trilean.Unknown || b == Trilean.Unknown ? Trilean.Unknown
    : Trilean.No;

  /// <summary>Negation: swaps <see cref="Trilean.Yes"/>/<see cref="Trilean.No"/>, fixes <see cref="Trilean.Unknown"/>.</summary>
  public static Trilean Not(Trilean a) =>
    a switch
    {
      Trilean.Yes => Trilean.No,
      Trilean.No => Trilean.Yes,
      _ => Trilean.Unknown,
    };
}

/// <summary>
/// A <see cref="ObjectFilterRelations.Subsumes"/> verdict with optional provenance: the axis that
/// decided it — the violated axis for <see cref="Trilean.No"/>, the first undecided axis for
/// <see cref="Trilean.Unknown"/>, <c>null</c> for <see cref="Trilean.Yes"/>.
/// </summary>
public readonly record struct SubsumeMatch(Trilean Value, string? Reason = null);
