using Flowthru.Core.Data;

namespace MagicAtlas.Data;

public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
