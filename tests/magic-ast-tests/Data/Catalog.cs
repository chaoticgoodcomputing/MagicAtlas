using Flowthru.Data.Catalog;
using MagicAST;

namespace MagicAtlas.Ast.Tests.Data;

/// <summary>
/// Data catalog for the MagicAST validation + triage flows. Layered by Kedro
/// convention — items defined in per-layer <c>Catalog.&lt;Layer&gt;.cs</c>
/// partial-class files under <c>Data/_NN_LayerName/</c>.
/// </summary>
public partial class Catalog : CatalogAbstract
{
  /// <summary>Filesystem root for all file-backed items, e.g. <c>tests/magic-ast-tests/Data</c>.</summary>
  private readonly string _basePath;

  /// <summary>Path to the ratchet baseline (cross-project today; consolidates post-merge of #7's blocker).</summary>
  private readonly string _ratchetBaselinePath;

  public Catalog(string basePath, string? ratchetBaselinePath = null)
  {
    _basePath = basePath;
    // Default: read the existing tools/test/magic-ast baseline. The path is resolved relative
    // to the data root, going up two levels to repo root then into tools/test/magic-ast/.
    _ratchetBaselinePath =
      ratchetBaselinePath
      ?? Path.GetFullPath(
        Path.Combine(basePath, "..", "..", "..", "tools", "test", "magic-ast", "test-baseline.json")
      );
  }

  /// <summary>Smoke-test output slot — populated by <c>MagicAstSmokeFlow</c>'s single source step.</summary>
  public IItem<ParseResult> SmokeParseResult =>
    CreateItem(() => Item.Of<ParseResult>("SmokeParseResult").Memory().Build());
}
