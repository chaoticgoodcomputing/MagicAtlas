using Flowthru.Data.Catalog;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Ast.Tests.Data;

/// <summary>Model-output layer: results of running the MagicAST parser over the corpus.</summary>
public partial class Catalog
{
  /// <summary>
  /// Per-card <see cref="ParseRecord"/>s emitted by <c>ParseCorpusStep</c>.
  /// Cached on disk so the aggregation step can re-run without re-parsing the
  /// full corpus.
  /// </summary>
  public IItem<IEnumerable<ParseRecord>> ParseRecords =>
    CreateItem(() => Item.Of<IEnumerable<ParseRecord>>("ParseRecords")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/parse-records.json")
      .Build());
}
