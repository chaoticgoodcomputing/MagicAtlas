using Flowthru.Data.Catalog;

namespace MagicAtlas.Data;

/// <summary>
/// Local-filesystem data catalog for the atlas pipelines.
/// </summary>
/// <remarks>
/// <para>
/// This is the development / data-science implementation: every raw, intermediate, and primary
/// item lives as a flat file under <c>{basePath}/_XX_Layer/Datasets/</c>. The <c>Ingest</c>
/// flow is what writes the <c>_01_Raw</c> items — there's no implicit HTTP auto-fetch at read
/// time; downstream flows assume Ingest has run (or run via a multi-flow slice like
/// <c>--to AtlasPoints</c>).
/// </para>
/// <para>
/// A future production catalog (EFCore-backed, consumed by Trax-orchestrated services) will
/// implement the same item surface but back it with database tables; flow code referencing
/// <see cref="Catalog"/> needs to stay swappable for that to happen, so don't slip
/// implementation details (paths, file extensions) into the call sites — keep them inside the
/// item factories on this class.
/// </para>
/// </remarks>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath;
  }
}
