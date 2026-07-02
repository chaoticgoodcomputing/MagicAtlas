namespace MagicAST.Interaction;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Loads the authored family-edge grammar (mast-interaction ADR-0001 §3 / §5 — declarative JSON,
/// versioned and judged). The grammar is the source of truth; the engine expands it over the derived
/// ports. <see cref="ResourceKind"/> / <see cref="EdgeFamily"/> serialize as strings.
/// </summary>
public static class FamilyGrammar
{
  private static readonly JsonSerializerOptions Options = new()
  {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() },
  };

  public static IReadOnlyList<FamilyEdge> Load(string path) =>
    JsonSerializer.Deserialize<List<FamilyEdge>>(File.ReadAllText(path), Options) ?? [];

  public static IReadOnlyList<FamilyEdge> Parse(string json) =>
    JsonSerializer.Deserialize<List<FamilyEdge>>(json, Options) ?? [];
}
