using Flowthru.Data;

namespace MagicAtlas.Data;

public partial class Catalog : DataCatalogBase
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
