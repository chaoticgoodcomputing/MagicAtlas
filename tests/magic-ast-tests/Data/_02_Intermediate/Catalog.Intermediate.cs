using Flowthru.Data.Catalog;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;

namespace MagicAtlas.Ast.Tests.Data;

/// <summary>Intermediate layer: cleaned, typed projections of raw data.</summary>
public partial class Catalog
{
  /// <summary>
  /// Per-card <see cref="MastCardInput"/> records — narrow projection of the
  /// Scryfall bulk into MagicAST's input contract. Cached on disk so the
  /// projection runs once per fetched bulk.
  /// </summary>
  public IItem<IEnumerable<MastCardInput>> CardInputs =>
    CreateItem(() => Item.Of<IEnumerable<MastCardInput>>("CardInputs")
      .Json()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/card-inputs.json")
      .Build());
}
