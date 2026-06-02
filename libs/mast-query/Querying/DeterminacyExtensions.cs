namespace MagicAST.Query;

using MagicAST.AST.References;

/// <summary>
/// Maps mast-query's <see cref="Determinacy"/> onto the canonical upstream three-valued primitive
/// <see cref="Trilean"/> (magic-ast, ADR-0008 open-Q2). The interaction engine composes "did the
/// port match?" (<see cref="Determinacy"/>) with "does the join hold?" (<c>FilterRelation</c>, the
/// same lattice) via one <see cref="Kleene"/> combinator rather than three ad-hoc enums:
/// <c>Match→Yes</c>, <c>NoMatch→No</c>, <c>Unknown→Unknown</c>.
/// </summary>
public static class DeterminacyExtensions
{
  public static Trilean ToTrilean(this Determinacy determinacy) =>
    determinacy switch
    {
      Determinacy.Match => Trilean.Yes,
      Determinacy.NoMatch => Trilean.No,
      _ => Trilean.Unknown,
    };
}
