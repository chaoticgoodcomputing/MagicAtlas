using System.Text.Json;
using System.Text.Json.Nodes;
using Flowthru.Step;

namespace MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// Source step (ADR-0003 Stage 0a → 0b): loads the negotiated topology scaffold
/// (<c>Fixtures/Interactions/topology-scaffold.json</c>) as a single semi-structured
/// <see cref="JsonNode"/>. This is the DECLARED half of <c>port-topology</c> — kinds, supergroups, bare
/// event verbs, the is-a stem spine, the closed attribute-axis licensing set, aliases, and the six
/// <c>witness:sought</c> holes. The <see cref="TopologyStep"/> merges it with the gold-witnessed half.
/// </summary>
[FlowthruStep]
public static class LoadTopologyScaffoldStep
{
  public static Func<JsonNode> Create(string scaffoldPath) =>
    () =>
    {
      JsonNode node;
      try
      {
        node = JsonNode.Parse(File.ReadAllText(scaffoldPath))
          ?? throw new InvalidOperationException($"topology scaffold parsed to null: {scaffoldPath}");
      }
      catch (JsonException e)
      {
        throw new InvalidOperationException(
          $"topology scaffold is not valid JSON ({scaffoldPath}): {e.Message}",
          e
        );
      }
      if (node is not JsonObject)
        throw new InvalidOperationException($"topology scaffold is not a JSON object: {scaffoldPath}");

      Console.Error.WriteLine($"[InteractionRollup] loaded topology scaffold from {scaffoldPath}");
      return node;
    };
}
