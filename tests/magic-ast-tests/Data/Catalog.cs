using Flowthru.Data.Catalog;
using MagicAST;

namespace MagicAtlas.Ast.Tests.Data;

/// <summary>
/// Catalog scaffold for MagicAST validation flows. Currently exposes a single in-memory item used
/// by the smoke flow; expand this catalog as real MagicAST test corpora and gold-standard ASTs
/// are added.
/// </summary>
public partial class Catalog : CatalogAbstract
{
  /// <summary>Smoke-test output slot — populated by <c>MagicAstSmokeFlow</c>'s single source step.</summary>
  public IItem<ParseResult> SmokeParseResult =>
    CreateItem(() => Item.Of<ParseResult>("SmokeParseResult").Memory().Build());
}
