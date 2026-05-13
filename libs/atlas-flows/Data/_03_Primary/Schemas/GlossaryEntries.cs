using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Flat dictionary of glossary terms and definitions.
/// </summary>
[FlowthruSchema]
public partial record GlossaryEntries
{
  [SerializedLabel("terms")]
  public Dictionary<string, string> Terms { get; init; } = new();
}
