using MagicAtlas.Data._03_Primary.Schemas;

namespace MagicAtlas.Pipelines.OracleEmbedding;

/// <summary>
/// Reduces <see cref="CardCoreData"/> to the Arrow-friendly <see cref="OracleInput"/> shape
/// expected by the Python embedding step. Drops fields that the Arrow marshaller can't handle
/// (decimals, nested records) and fields the Python code doesn't need.
/// </summary>
public static class ProjectOracleInputNode
{
  public static Func<IEnumerable<CardCoreData>, Task<IEnumerable<OracleInput>>> Create() =>
    cards =>
      Task.FromResult<IEnumerable<OracleInput>>(
        cards
          .Where(c => !string.IsNullOrWhiteSpace(c.OracleText))
          .Select(c => new OracleInput { Id = c.Id, OracleText = c.OracleText ?? "" })
          .ToList()
      );
}
