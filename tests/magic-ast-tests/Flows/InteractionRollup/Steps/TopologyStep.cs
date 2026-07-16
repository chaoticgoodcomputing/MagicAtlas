using System.Text.Json;
using System.Text.Json.Nodes;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// Builds artifact 1 — the port topology (ADR-0003 §8): the stem universe (is-a parent + Event/State/
/// Behavior kind + observed attribute set), the kinds map, and the attribute-axis value lattices. Emits
/// the lean <see cref="PortTopology"/> and its cited twin (same entries + per-stem witnesses); the lean is
/// literally the cited with provenance nulled (the serializer omits nulls). Ports the Python prototype's
/// topology construction verbatim, including <c>str()</c>-style value stringification (booleans render
/// <c>True</c>/<c>False</c>) so the value lattices match byte-for-byte.
/// </summary>
[FlowthruStep]
public static class TopologyStep
{
  private const string GeneratedStamp = "tools/interaction-rollup";

  private sealed class StemAccum
  {
    public required string Kind { get; init; }
    public required string? Parent { get; init; }
    public SortedSet<string> Attrs { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> Witnesses { get; } = new(StringComparer.Ordinal);
  }

  private sealed class AxisAccum
  {
    public SortedSet<string> Stems { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> Values { get; } = new(StringComparer.Ordinal);
    public bool ProvenanceOrPolarity { get; set; }
  }

  public static Func<IEnumerable<JsonNode>, (PortTopology, PortTopology)> Create() =>
    golds =>
    {
      var goldList = golds.ToList();
      var goldIds = goldList
        .Select(g => g!.AsObject()["id"]!.GetValue<string>())
        .OrderBy(x => x, StringComparer.Ordinal)
        .ToList();

      // Kinds seeded in EVENT/STATE/BEHAVIOR order (mirrors the prototype); extras appended on encounter.
      var kinds = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal)
      {
        ["EVENT"] = new(StringComparer.Ordinal),
        ["STATE"] = new(StringComparer.Ordinal),
        ["BEHAVIOR"] = new(StringComparer.Ordinal),
      };
      var stems = new Dictionary<string, StemAccum>(StringComparer.Ordinal);
      var axes = new Dictionary<string, AxisAccum>(StringComparer.Ordinal);

      foreach (var gn in goldList)
      {
        var g = gn!.AsObject();
        var gid = g["id"]!.GetValue<string>();
        foreach (var cardKv in g["ports"]!.AsObject())
        {
          if (cardKv.Value is not JsonArray plist)
            continue;
          foreach (var pn in plist)
          {
            var p = pn!.AsObject();
            var stem = p["stem"]!.GetValue<string>();
            var kind = p["kind"]!.GetValue<string>();

            if (!kinds.TryGetValue(kind, out var kset))
              kinds[kind] = kset = new SortedSet<string>(StringComparer.Ordinal);
            kset.Add(stem);

            if (!stems.TryGetValue(stem, out var s))
            {
              var colon = stem.LastIndexOf(':');
              stems[stem] = s = new StemAccum
              {
                Kind = kind,
                Parent = colon >= 0 ? stem[..colon] : null,
              };
            }
            s.Witnesses.Add(gid);

            foreach (var akv in p["attrs"]!.AsObject())
            {
              s.Attrs.Add(akv.Key);
              if (!axes.TryGetValue(akv.Key, out var ax))
                axes[akv.Key] = ax = new AxisAccum();
              ax.Stems.Add(stem);

              string valueStr;
              if (akv.Value is JsonObject valObj)
              {
                ax.ProvenanceOrPolarity = true;
                valueStr = Stringify(valObj["value"]);
              }
              else
              {
                valueStr = Stringify(akv.Value);
              }
              ax.Values.Add(valueStr);
            }
          }
        }
      }

      var kindsOut = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
      foreach (var kv in kinds)
        if (kv.Value.Count > 0)
          kindsOut[kv.Key] = kv.Value.ToList();

      var axesOut = new Dictionary<string, AxisEntry>(StringComparer.Ordinal);
      foreach (var kv in axes.OrderBy(k => k.Key, StringComparer.Ordinal))
        axesOut[kv.Key] = new AxisEntry
        {
          Stems = kv.Value.Stems.ToList(),
          ValuesSeen = kv.Value.Values.ToList(),
          CarriesProvenanceOrPolarity = kv.Value.ProvenanceOrPolarity,
        };

      var stemsLean = new Dictionary<string, StemEntry>(StringComparer.Ordinal);
      var stemsCited = new Dictionary<string, StemEntry>(StringComparer.Ordinal);
      foreach (var kv in stems.OrderBy(k => k.Key, StringComparer.Ordinal))
      {
        var attrs = kv.Value.Attrs.ToList();
        stemsLean[kv.Key] = new StemEntry
        {
          Kind = kv.Value.Kind,
          Parent = kv.Value.Parent,
          Status = "witnessed",
          Attrs = attrs,
        };
        stemsCited[kv.Key] = new StemEntry
        {
          Kind = kv.Value.Kind,
          Parent = kv.Value.Parent,
          Status = "witnessed",
          Attrs = attrs,
          Witnesses = kv.Value.Witnesses.ToList(),
        };
      }

      var lean = new PortTopology
      {
        Generated = GeneratedStamp,
        Golds = goldIds,
        Kinds = kindsOut,
        Stems = stemsLean,
        AttributeAxes = axesOut,
      };
      var cited = lean with { Stems = stemsCited };
      return (lean, cited);
    };

  /// <summary>Reproduces Python's <c>str(av)</c>: <c>True</c>/<c>False</c> for booleans, the raw numeric
  /// text for numbers, the string itself for strings, and <c>None</c> for a null/absent value.</summary>
  private static string Stringify(JsonNode? node)
  {
    if (node is null)
      return "None";
    if (node is JsonValue v && v.TryGetValue<JsonElement>(out var el))
    {
      return el.ValueKind switch
      {
        JsonValueKind.True => "True",
        JsonValueKind.False => "False",
        JsonValueKind.String => el.GetString()!,
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.Null => "None",
        _ => el.GetRawText(),
      };
    }
    return node.ToJsonString();
  }
}
