using Flowthru.Data.Catalog;

namespace MagicAtlas.Rules.Data;

/// <summary>
/// Local-filesystem data catalog for the MTG rules pipeline. Every raw, intermediate, and
/// primary item lives as a flat file under <c>{basePath}/_XX_Layer/Datasets/</c>.
/// </summary>
/// <remarks>
/// This project is the single producer of the structured-rules and type-ontology artifacts.
/// Consumers (MAST's tests, MagicAtlas' atlas-flows) vendor the produced artifacts into their
/// own trees rather than reaching across project boundaries — the rules text itself (WotC
/// copyright) never leaves this project's <c>_01_Raw</c> layer, only the derived facts do.
/// </remarks>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath;
  }
}
