namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;
using MagicAST.AST.References;

/// <summary>
/// One <c>Intersects</c> conformance case: two <c>ObjectFilter</c> JSON blobs and the
/// <see cref="FilterRelation"/> the operator must produce for them.
/// </summary>
public sealed class FilterRelationTestCase
{
  public required string Name { get; init; }
  public required JsonNode A { get; init; }
  public required JsonNode B { get; init; }
  public required FilterRelation Expected { get; init; }

  public override string ToString() => Name;
}
