using System.Text.Json.Nodes;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// Builds artifact 2 — the port-interaction residual rules (ADR-0003 §8), and runs the two Stage-0b gates:
/// <list type="number">
///   <item><b>Rule-union conflict.</b> Rules are unioned by id across golds; the same id declaring a
///     different core (the fields that DEFINE the rule) is a conflict → the step throws → the flow fails.
///     This IS the "conflicts fail the build" gate.</item>
///   <item><b>Ladder coherence.</b> A rule climbs <c>observed</c> (1 witness) → <c>corroborated</c> (≥2) →
///     <c>confirmed</c> (a witnessing gold judge-PASSed). A GREEN edge must rest on a <c>confirmed</c>
///     rule; otherwise the step throws. Every non-structural edge must also cite a rule that exists in
///     the union.</item>
/// </list>
/// Emits the lean <see cref="PortInteractions"/> and its cited twin; lean is the cited with the provenance
/// fields (witnesses / desc / cr / corroborates) nulled — omitted by the WhenWritingNull serializer.
/// </summary>
[FlowthruStep]
public static class InteractionsStep
{
  private const string GeneratedStamp = "tools/interaction-rollup";

  private static readonly string[] Sections = { "polarity", "match_policy", "guards", "bridges" };
  private static readonly HashSet<string> Structural = new(StringComparer.Ordinal)
  {
    "subsumption",
    "card-defined",
    "modifier",
  };

  // Fields that DEFINE a rule per section (a mismatch here across golds is a conflict).
  private static readonly Dictionary<string, string[]> Core = new(StringComparer.Ordinal)
  {
    ["polarity"] = new[] { "attr", "context", "value" },
    ["match_policy"] = new[] { "consume_kind", "subject" },
    ["guards"] = new[] { "impl" },
    ["bridges"] = new[] { "from_stem", "to_stem", "ceiling" },
  };

  private sealed class RuleSlot
  {
    public required JsonObject Rule { get; init; }
    public required string CoreKey { get; init; }
    public required string CoreDisplay { get; init; }
    public SortedSet<string> Witnesses { get; } = new(StringComparer.Ordinal);
  }

  public static Func<IEnumerable<JsonNode>, (PortInteractions, PortInteractions)> Create() =>
    golds =>
    {
      var goldList = golds.Select(g => g!.AsObject()).ToList();
      var goldsById = goldList.ToDictionary(g => g["id"]!.GetValue<string>(), g => g, StringComparer.Ordinal);
      var goldIds = goldsById.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

      var errors = new List<string>();
      var conflicts = new List<string>();

      // ── union rules (with conflict detection + witnesses) ──
      var rules = Sections.ToDictionary(
        s => s,
        _ => new Dictionary<string, RuleSlot>(StringComparer.Ordinal),
        StringComparer.Ordinal
      );
      foreach (var g in goldList)
      {
        var gid = g["id"]!.GetValue<string>();
        if (g["declares"] is not JsonObject declares)
          continue;
        foreach (var sec in Sections)
        {
          if (declares[sec] is not JsonArray arr)
            continue;
          foreach (var rn in arr)
          {
            var r = rn!.AsObject();
            var rid = r["id"]!.GetValue<string>();
            var (coreKey, coreDisplay) = CoreOf(r, sec);
            if (!rules[sec].TryGetValue(rid, out var slot))
            {
              slot = new RuleSlot { Rule = r, CoreKey = coreKey, CoreDisplay = coreDisplay };
              slot.Witnesses.Add(gid);
              rules[sec][rid] = slot;
            }
            else if (slot.CoreKey != coreKey)
            {
              conflicts.Add(
                $"{sec}:{rid} — {gid} declares {coreDisplay} but a prior gold declared {slot.CoreDisplay}"
              );
            }
            else
            {
              slot.Witnesses.Add(gid);
            }
          }
        }
      }

      var allRuleIds = new HashSet<string>(
        rules.Values.SelectMany(d => d.Keys),
        StringComparer.Ordinal
      );

      string? StatusOf(string rid)
      {
        foreach (var sec in Sections)
          if (rules[sec].TryGetValue(rid, out var slot))
            return RuleStatus(slot.Witnesses, goldsById);
        return null;
      }

      // ── external rule existence + ladder coherence ──
      foreach (var g in goldList)
      {
        var gid = g["id"]!.GetValue<string>();
        var edges = g["edges"] as JsonArray ?? new JsonArray();

        foreach (var en in edges)
        {
          var e = en!.AsObject();
          var mech = e["mechanism"]?.GetValue<string>();
          if (mech is not null && !Structural.Contains(mech))
          {
            var rule = e["rule"]?.GetValue<string>();
            if (string.IsNullOrEmpty(rule))
              errors.Add($"{gid}: edge {e["id"]} mechanism={mech} must cite a 'rule'");
            else if (!allRuleIds.Contains(rule))
              errors.Add($"{gid}: edge {e["id"]} cites rule {rule} declared by no gold");
          }
        }

        var loopTier = g["loop_tier"]?.GetValue<string>();
        var anyGreen =
          loopTier == "GREEN"
          || edges.Any(en => en!.AsObject()["tier"]?.GetValue<string>() == "GREEN");
        if (anyGreen)
        {
          foreach (var en in edges)
          {
            var e = en!.AsObject();
            if (e["tier"]?.GetValue<string>() == "GREEN" && e["rule"] is not null)
            {
              var rule = e["rule"]!.GetValue<string>();
              var st = StatusOf(rule);
              if (st != "confirmed")
                errors.Add(
                  $"{gid}: GREEN edge {e["id"]} rests on rule {rule} (status={st ?? "none"}) — "
                    + "only 'confirmed' rules may certify GREEN"
                );
            }
          }
        }
      }

      if (conflicts.Count > 0 || errors.Count > 0)
        throw new InvalidOperationException(
          "Interaction rollup failed:\n  " + string.Join("\n  ", errors.Concat(conflicts))
        );

      // ── build rule lists ──
      List<RuleEntry> RulesOf(string sec, bool cited) =>
        rules[sec]
          .Keys.OrderBy(x => x, StringComparer.Ordinal)
          .Select(rid => Rule(sec, rid, rules[sec][rid], goldsById, cited))
          .ToList();

      PortInteractions Build(bool cited) =>
        new()
        {
          Generated = GeneratedStamp,
          Golds = goldIds,
          Conflicts = conflicts, // empty when emitted (else we threw above)
          Polarity = RulesOf("polarity", cited),
          MatchPolicy = RulesOf("match_policy", cited),
          Guards = RulesOf("guards", cited),
          Bridges = RulesOf("bridges", cited),
        };

      return (Build(cited: false), Build(cited: true));
    };

  private static RuleEntry Rule(
    string sec,
    string rid,
    RuleSlot slot,
    IReadOnlyDictionary<string, JsonObject> goldsById,
    bool cited
  )
  {
    var r = slot.Rule;
    var entry = new RuleEntry
    {
      Id = rid,
      Attr = sec == "polarity" ? Str(r["attr"]) : null,
      Context = sec == "polarity" ? Str(r["context"]) : null,
      Value = sec == "polarity" ? Str(r["value"]) : null,
      ConsumeKind = sec == "match_policy" ? Str(r["consume_kind"]) : null,
      Subject = sec == "match_policy" ? Str(r["subject"]) : null,
      Impl = sec == "guards" ? Str(r["impl"]) : null,
      FromStem = sec == "bridges" ? Str(r["from_stem"]) : null,
      ToStem = sec == "bridges" ? Str(r["to_stem"]) : null,
      Ceiling = sec == "bridges" ? Str(r["ceiling"]) : null,
      Status = RuleStatus(slot.Witnesses, goldsById),
      FromAttrs = sec == "bridges" ? DictOf(r["from_attrs"]) : null,
    };
    if (!cited)
      return entry;
    return entry with
    {
      Witnesses = slot.Witnesses.OrderBy(x => x, StringComparer.Ordinal).ToList(),
      Desc = Str(r["desc"]),
      Cr = ListOf(r["cr"]),
      Corroborates = Str(r["corroborates"]),
    };
  }

  /// <summary>observed(1) → corroborated(≥2) → confirmed(any witness judge-PASSed).</summary>
  private static string RuleStatus(
    SortedSet<string> witnesses,
    IReadOnlyDictionary<string, JsonObject> goldsById
  )
  {
    var confirmed = witnesses.Any(w =>
      goldsById.TryGetValue(w, out var g)
      && g["judge"]?["verdict"]?.GetValue<string>() == "PASS"
    );
    if (confirmed)
      return "confirmed";
    return witnesses.Count >= 2 ? "corroborated" : "observed";
  }

  /// <summary>Canonical string of a rule's core fields — used both for conflict detection (CoreKey) and
  /// the human-readable conflict message (Display).</summary>
  private static (string Key, string Display) CoreOf(JsonObject r, string sec)
  {
    var parts = Core[sec].Select(k => $"{k}={r[k]?.ToJsonString() ?? "null"}").ToList();
    return (string.Join("", parts), "{" + string.Join(", ", parts) + "}");
  }

  private static string? Str(JsonNode? node)
  {
    if (node is null)
      return null;
    if (node is JsonValue v && v.TryGetValue<string>(out var s))
      return s;
    return node.ToJsonString();
  }

  private static IReadOnlyList<string>? ListOf(JsonNode? node)
  {
    if (node is not JsonArray arr)
      return null;
    return arr.Select(x => x!.GetValue<string>()).ToList();
  }

  private static IReadOnlyDictionary<string, string>? DictOf(JsonNode? node)
  {
    if (node is not JsonObject obj)
      return null;
    var d = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var kv in obj)
      d[kv.Key] = Str(kv.Value) ?? "";
    return d;
  }
}
