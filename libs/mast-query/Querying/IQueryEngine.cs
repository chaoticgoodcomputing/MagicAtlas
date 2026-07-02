namespace MagicAST.Query;

using System.Text.Json.Nodes;
using MagicAST.Query.Patterns;

/// <summary>A card AST in canonical JSON form, addressed by display name (the card id in the real corpus).</summary>
public sealed record CardDocument(string Card, JsonNode Ast);

/// <summary>
/// Runs a query pattern over a corpus, returning a three-valued result (mast-query ADR-0001).
/// The contract is engine-agnostic: any conforming implementation must reproduce the conformance
/// suite. The reference engine is <see cref="FilterAndVerifyEngine"/>.
/// </summary>
public interface IQueryEngine
{
  QueryResult Run(string queryName, Pattern pattern, IReadOnlyList<CardDocument> corpus);
}
