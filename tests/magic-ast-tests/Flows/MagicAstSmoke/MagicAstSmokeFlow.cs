using Flowthru.Flow;
using MagicAST;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstSmoke;

/// <summary>
/// Placeholder smoke flow that proves the MagicAST package is wired up under Flowthru's pipeline
/// machinery. Currently runs <see cref="OracleParser"/> against a hardcoded oracle text and parks
/// the result in an in-memory item. Replace with real corpus-driven validation flows as the
/// MagicAST parsers mature.
/// </summary>
public static class MagicAstSmokeFlow
{
  private const string SmokeOracleText = "Flying\nVigilance";

  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("MagicAstSmoke", pipeline =>
    {
      pipeline.AddStep<ParseResult>(
        label: "ParseSmokeOracle",
        transform: () => new OracleParser().Parse(SmokeOracleText),
        outputs: catalog.SmokeParseResult
      );
    });
  }
}
