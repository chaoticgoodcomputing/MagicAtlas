using System.Text.Json;
using System.Text.Json.Nodes;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// Builds artifact 1 — the port topology (ADR-0003 §8), now the MERGE of the negotiated Stage-0a scaffold
/// (the DECLARED half) with the gold-projected ports (the WITNESSED half):
/// <list type="bullet">
///   <item><c>kinds</c> / <c>supergroups</c> / <c>event_verbs</c> / <c>aliases</c> — passed through from
///     the scaffold verbatim.</item>
///   <item><c>holes</c> — scaffold-declared, but <c>status</c> is reconciled against the same
///     gold-projection pass that computes <c>stems</c>: <c>sought</c> until some gold projects the hole's
///     <c>proposed_stem</c>, then <c>witnessed</c> (with the witnessing gold ids carried on the hole). A
///     hole does not stay <c>sought</c> forever just because the scaffold entry is static.</item>
///   <item><c>stems</c> — the scaffold's is-a spine (declared) unioned with the stems the golds project
///     (witnessed). Per stem <c>status</c> is <c>witnessed</c> when any gold projects it, else
///     <c>declared</c>; a gold stem the scaffold never predicted carries <c>unpredicted: true</c>.</item>
///   <item><c>attribute_axes</c> — the scaffold's closed licensing/lattice set unioned with the golds'
///     witnessed stems + value lattices.</item>
/// </list>
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
    public bool Declared { get; set; }
    public bool Projected { get; set; }
    public SortedSet<string> Attrs { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> Witnesses { get; } = new(StringComparer.Ordinal);
  }

  private sealed class AxisAccum
  {
    public SortedSet<string> Stems { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> Values { get; } = new(StringComparer.Ordinal);
    public bool ProvenanceOrPolarity { get; set; }
    public JsonObject? Declared { get; set; }
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

      // ── scaffold pass-through sections ──
      var kinds = ScaffoldStringMap(scaffold["kinds"]);
      var aliases = ScaffoldStringMap(scaffold["aliases"]);

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

      // Deferred: a hole's status depends on whether its proposed_stem ends up projected by a gold, which
      // isn't known until the gold-projection pass below runs. Stash the scaffold entries now, materialize
      // HoleEntry after `stems` is fully populated.
      var holeScaffold = Entries(scaffold["holes"]).ToList();

      // ── stems: scaffold spine (declared) ∪ gold projections (witnessed) ──
      var stems = new Dictionary<string, StemAccum>(StringComparer.Ordinal);
      foreach (var kv in Entries(scaffold["stems_representative"]))
      {
        var s = kv.Value;
        stems[kv.Key] = new StemAccum
        {
          Kind = s["kind"]!.GetValue<string>(),
          Parent = s["parent"]?.GetValue<string>() ?? DeriveParent(kv.Key),
          Declared = true,
        };
      }

      // ── attribute axes: scaffold closed set (declared) ∪ gold values (witnessed) ──
      var axes = new Dictionary<string, AxisAccum>(StringComparer.Ordinal);
      foreach (var kv in Entries(scaffold["attribute_axes"]))
        axes[kv.Key] = new AxisAccum { Declared = kv.Value };

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
            {
              // Gold stem the scaffold never predicted.
              stems[stem] = s = new StemAccum { Kind = kind, Parent = DeriveParent(stem) };
            }
            s.Projected = true;
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

      // ── materialize holes: resolved once a gold projects the proposed_stem, not hardcoded "sought" ──
      var holes = new Dictionary<string, HoleEntry>(StringComparer.Ordinal);
      foreach (var kv in holeScaffold)
      {
        var proposedStem = kv.Value["proposed_stem"]!.GetValue<string>();
        var resolved = stems.TryGetValue(proposedStem, out var stemAccum) && stemAccum.Projected;
        holes[kv.Key] = new HoleEntry
        {
          Priority = kv.Value["priority"]!.GetValue<int>(),
          Kind = kv.Value["kind"]!.GetValue<string>(),
          ProposedStem = proposedStem,
          Attrs = StrListOrNull(kv.Value["attrs"]),
          Slang = StrListOrNull(kv.Value["slang"]),
          Note = kv.Value["note"]?.GetValue<string>(),
          Status = resolved ? "witnessed" : "sought",
          Witnesses = resolved ? stemAccum!.Witnesses.ToList() : null,
        };
      }

      // ── materialize axes ──
      var axesOut = new Dictionary<string, AxisEntry>(StringComparer.Ordinal);
      foreach (var kv in axes.OrderBy(k => k.Key, StringComparer.Ordinal))
      {
        var d = kv.Value.Declared;
        axesOut[kv.Key] = new AxisEntry
        {
          Stems = kv.Value.Stems.ToList(),
          ValuesSeen = kv.Value.Values.ToList(),
          CarriesProvenanceOrPolarity = kv.Value.ProvenanceOrPolarity,
          LicensedBy = StrListOrNull(d?["licensed_by"]),
          Lattice = d?["lattice"]?.GetValue<string>(),
          Enum = StringifyListOrNull(d?["enum"]),
          Bindable = StrListOrNull(d?["bindable"]),
          Kind = d?["kind"]?.GetValue<string>(),
          Note = d?["note"]?.GetValue<string>(),
        };
      }

      // ── materialize stems (lean + cited) ──
      var stemsLean = new Dictionary<string, StemEntry>(StringComparer.Ordinal);
      var stemsCited = new Dictionary<string, StemEntry>(StringComparer.Ordinal);
      foreach (var kv in stems.OrderBy(k => k.Key, StringComparer.Ordinal))
      {
        var a = kv.Value;
        var attrs = a.Attrs.ToList();
        var status = a.Projected ? "witnessed" : "declared";
        bool? unpredicted = a.Projected && !a.Declared ? true : null;

        stemsLean[kv.Key] = new StemEntry
        {
          Kind = a.Kind,
          Parent = a.Parent,
          Status = status,
          Attrs = attrs,
          Unpredicted = unpredicted,
        };
        stemsCited[kv.Key] = new StemEntry
        {
          Kind = a.Kind,
          Parent = a.Parent,
          Status = status,
          Attrs = attrs,
          Unpredicted = unpredicted,
          Witnesses = a.Witnesses.Count > 0 ? a.Witnesses.ToList() : null,
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
        Aliases = aliases,
        Holes = holes,
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

  private static IReadOnlyList<string>? StrListOrNull(JsonNode? node) =>
    node is JsonArray arr ? arr.Select(x => x!.GetValue<string>()).ToList() : null;

  private static IReadOnlyList<string>? StringifyListOrNull(JsonNode? node) =>
    node is JsonArray arr ? arr.Select(Stringify).ToList() : null;

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
