using Flowthru.Core.Abstractions;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// The minimal oracle-text payload fed to the Python embedding step.
/// Deliberately Arrow-friendly (no <c>decimal</c>, no nested types) because the row
/// ships to a Python subprocess via Apache Arrow.
/// </summary>
public record OracleInput : IStructuredSerializable, IFlatSchema
{
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  [SerializedLabel("oracle_text")]
  public string OracleText { get; init; } = "";
}
