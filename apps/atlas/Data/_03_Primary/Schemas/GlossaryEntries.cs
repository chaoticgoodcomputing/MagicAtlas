using Flowthru.Abstractions;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Flat dictionary of glossary terms and definitions.
/// </summary>
public record GlossaryEntries : IStructuredSerializable
{
  [SerializedLabel("terms")]
  public Dictionary<string, string> Terms { get; init; } = new();
}
