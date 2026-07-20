using System.Text.Json;
using System.Text.Json.Nodes;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// Builds artifact 1 — the port topology (ADR-0003 §8) — now 100% WITNESS-DERIVED (ADR-0004 §7, issue #26).
/// <list type="bullet">
///   <item><c>kinds</c> / <c>supergroups</c> / <c>event_verbs</c> — passed through from the scaffold
///     verbatim. These are the vocabulary NO gold can witness (a gold declares a port's stem, never a kind
///     definition or a supergroup), so they remain declared input.</item>
///   <item><c>stems</c> — exactly the stems the golds project. Every entry is <c>status: witnessed</c> with
///     its witnessing gold ids; the scaffold's declared <c>stems_representative</c> spine is DELETED (it was
///     strictly subsumed: 0 declared-but-unwitnessed, 12 witnessed-but-never-predicted), and with it the
///     <c>unpredicted</c> flag, which was a claim about a prediction set that no longer exists.</item>
///   <item><c>attribute_axes</c> — exactly the axes the golds' ports carry, with the stems carrying them and
///     the value lattice witnessed. The scaffold's declared closed sets (<c>licensed_by</c>/<c>enum</c>/
///     <c>lattice</c>/<c>bindable</c>) are DELETED, along with the validator that cross-checked them —
///     without a declared half there is no drift to detect.</item>
/// </list>
/// <c>aliases</c> (slang → attribute-constraint query) and <c>holes</c> (the sought-stem backlog) are gone
/// from the artifact entirely: the first was a pure scaffold pass-through nothing consumed, the second was a
/// hand-typed <c>status</c> inside a generated file — the exact failure ADR-0004 exists to end. The backlog
/// is a set difference (declared − witnessed), computed where it is asked for, per ADR-0004 §2 / issue #32.
/// Emits the lean <see cref="PortTopology"/> and its cited twin (same entries + per-stem witnesses); lean
/// is the cited with provenance nulled (the WhenWritingNull serializer omits it). Value stringification
/// mirrors Python's <c>str()</c> (booleans render <c>True</c>/<c>False</c>).
/// </summary>
[FlowthruStep]
public static class TopologyStep
{
  private const string GeneratedStamp = "tools/interaction-rollup";

  private sealed class StemAccum
  {
    public required string Kind { get; set; }
    public required string? Parent { get; set; }
    public SortedSet<string> Attrs { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> Witnesses { get; } = new(StringComparer.Ordinal);
  }

  private sealed class AxisAccum
  {
    public SortedSet<string> Stems { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> Values { get; } = new(StringComparer.Ordinal);
    public bool ProvenanceOrPolarity { get; set; }
  }

  public static Func<(IEnumerable<JsonNode> Golds, JsonNode Scaffold), (PortTopology, PortTopology)> Create() =>
    input =>
    {
      var goldList = input.Golds.ToList();
      var scaffold = input.Scaffold.AsObject();

      var goldIds = goldList
        .Select(g => g!.AsObject()["id"]!.GetValue<string>())
        .OrderBy(x => x, StringComparer.Ordinal)
        .ToList();

      // ── scaffold pass-through sections (the un-witnessable vocabulary only) ──
      var kinds = ScaffoldStringMap(scaffold["kinds"]);

      var supergroups = new Dictionary<string, SupergroupEntry>(StringComparer.Ordinal);
      foreach (var kv in Entries(scaffold["supergroups"]))
        supergroups[kv.Key] = new SupergroupEntry
        {
          KindView = kv.Value["kind_view"]!.GetValue<string>(),
          Def = kv.Value["def"]!.GetValue<string>(),
        };

      var eventVerbs = new Dictionary<string, EventVerbEntry>(StringComparer.Ordinal);
      foreach (var kv in Entries(scaffold["event_verbs_no_supergroup"]))
        eventVerbs[kv.Key] = new EventVerbEntry
        {
          Kind = kv.Value["kind"]!.GetValue<string>(),
          Def = kv.Value["def"]!.GetValue<string>(),
        };

      // ── stems + attribute axes: PURELY gold-projected (no declared seed) ──
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

            if (!stems.TryGetValue(stem, out var s))
              stems[stem] = s = new StemAccum { Kind = kind, Parent = DeriveParent(stem) };
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

      // ── materialize axes ──
      var axesOut = new Dictionary<string, AxisEntry>(StringComparer.Ordinal);
      foreach (var kv in axes.OrderBy(k => k.Key, StringComparer.Ordinal))
        axesOut[kv.Key] = new AxisEntry
        {
          Stems = kv.Value.Stems.ToList(),
          ValuesSeen = kv.Value.Values.ToList(),
          CarriesProvenanceOrPolarity = kv.Value.ProvenanceOrPolarity,
        };

      // ── materialize stems (lean + cited) ──
      var stemsLean = new Dictionary<string, StemEntry>(StringComparer.Ordinal);
      var stemsCited = new Dictionary<string, StemEntry>(StringComparer.Ordinal);
      foreach (var kv in stems.OrderBy(k => k.Key, StringComparer.Ordinal))
      {
        var a = kv.Value;
        var attrs = a.Attrs.ToList();

        // A stem is in `stems` exactly when a gold projected it, so the status is invariantly "witnessed".
        // Kept as a field (not dropped) because it is what the executable `stem.<S>.witnessed` gold claims
        // read; TopologyRollupContractTests pins the invariant so no other value can ever appear.
        stemsLean[kv.Key] = new StemEntry
        {
          Kind = a.Kind,
          Parent = a.Parent,
          Status = "witnessed",
          Attrs = attrs,
        };
        stemsCited[kv.Key] = new StemEntry
        {
          Kind = a.Kind,
          Parent = a.Parent,
          Status = "witnessed",
          Attrs = attrs,
          Witnesses = a.Witnesses.ToList(),
        };
      }

      var lean = new PortTopology
      {
        Generated = GeneratedStamp,
        Golds = goldIds,
        Kinds = kinds,
        Supergroups = supergroups,
        EventVerbs = eventVerbs,
        Stems = stemsLean,
        AttributeAxes = axesOut,
      };
      var cited = lean with { Stems = stemsCited };
      return (lean, cited);
    };

  /// <summary>The is-a parent: the stem up to the last <c>:</c>, or null for a top-level stem.</summary>
  private static string? DeriveParent(string stem)
  {
    var colon = stem.LastIndexOf(':');
    return colon >= 0 ? stem[..colon] : null;
  }

  /// <summary>Enumerate an object's entries, skipping <c>$</c>-prefixed metadata keys (e.g. <c>$note</c>).</summary>
  private static IEnumerable<KeyValuePair<string, JsonObject>> Entries(JsonNode? node)
  {
    if (node is not JsonObject obj)
      yield break;
    foreach (var kv in obj)
    {
      if (kv.Key.StartsWith('$'))
        continue;
      if (kv.Value is JsonObject v)
        yield return new(kv.Key, v);
    }
  }

  /// <summary>A dict-of-strings pass-through, skipping <c>$</c>-prefixed metadata keys.</summary>
  private static Dictionary<string, string> ScaffoldStringMap(JsonNode? node)
  {
    var d = new Dictionary<string, string>(StringComparer.Ordinal);
    if (node is JsonObject obj)
      foreach (var kv in obj)
        if (!kv.Key.StartsWith('$') && kv.Value is not null)
          d[kv.Key] = kv.Value.GetValue<string>();
    return d;
  }

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
