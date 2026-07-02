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

  /// <summary>
  /// Lean interaction-triage combos — the Commander Spellbook dump projected to
  /// <see cref="Combo"/> (cards + popularity + results), the ~510 MB raw stripped to what triage
  /// uses. Cached on disk so the projection runs once per fetched dump.
  /// </summary>
  public IItem<IEnumerable<Combo>> Combos =>
    CreateItem(() => Item.Of<IEnumerable<Combo>>("Combos")
      .Json()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/combos.json")
      .Build());
}
