namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;
using MagicAST.AST.References;

/// <summary>
/// One <c>Subsumes</c> conformance case: a <c>sub</c> and <c>sup</c> <c>ObjectFilter</c> and the
/// <see cref="Trilean"/> the directional containment <c>sub ⊆ sup</c> must produce.
/// </summary>
public sealed class SubsumptionTestCase
{
  public required string Name { get; init; }
  public required JsonNode Sub { get; init; }
  public required JsonNode Sup { get; init; }
  public required Trilean Expected { get; init; }

  public override string ToString() => Name;
}
