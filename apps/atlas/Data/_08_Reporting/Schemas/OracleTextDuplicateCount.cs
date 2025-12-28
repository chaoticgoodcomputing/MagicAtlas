using Flowthru.Abstractions;

namespace MagicAtlas.Data._08_Reporting.Schemas;

public record OracleTextDuplicateCount
  : IStructuredSerializable,
    INestedSerializable,
    IFlatSerializable
{
  /// <summary>
  /// Oracle text.
  /// </summary>
  public required string OracleText { get; init; }

  /// <summary>
  /// Number of duplicate entries with the same oracle text.
  /// </summary>
  public required int DuplicateCount { get; init; }
}
