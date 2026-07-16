using System.Text.Json;
using System.Text.Json.Nodes;
using Flowthru.Step;

namespace MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// Source step (ADR-0003 §8, Stage 0b): reads the hand-authored interaction golds from
/// <c>Fixtures/Interactions/golds/*.json</c> as semi-structured <see cref="JsonNode"/>s and runs the
/// per-gold structural validation the Python prototype's <c>validate()</c> performed — required keys,
/// unique port ids per card, required port fields, unique + resolvable edges, and a cited rule for every
/// non-structural edge mechanism. Any well-formedness failure throws, which fails the whole flow before
/// any artifact is written (all-or-nothing, mirroring the prototype's refusal to write on error).
/// The rule-union conflict + ladder-coherence gates run downstream in <see cref="InteractionsStep"/>.
/// </summary>
[FlowthruStep]
public static class LoadInteractionGoldsStep
{
  private static readonly string[] Sections = { "polarity", "match_policy", "guards", "bridges" };
  private static readonly HashSet<string> Structural = new(StringComparer.Ordinal)
  {
    "subsumption",
    "card-defined",
    "modifier",
  };

  public static Func<IEnumerable<JsonNode>> Create(string goldsDir) =>
    () =>
    {
      var golds = new List<JsonNode>();
      var errors = new List<string>();

      var files = Directory
        .EnumerateFiles(goldsDir, "*.json", SearchOption.TopDirectoryOnly)
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToList();

      foreach (var path in files)
      {
        JsonNode? node;
        try
        {
          node = JsonNode.Parse(File.ReadAllText(path));
        }
        catch (JsonException e)
        {
          errors.Add($"{Path.GetFileName(path)}: unparseable JSON — {e.Message}");
          continue;
        }
        if (node is not JsonObject)
        {
          errors.Add($"{Path.GetFileName(path)}: not a JSON object");
          continue;
        }
        golds.Add(node);
        Validate(Path.GetFileName(path), node.AsObject(), errors);
      }

      if (golds.Count == 0)
        errors.Add("no golds found");

      if (errors.Count > 0)
        throw new InvalidOperationException(
          "Interaction golds failed structural validation:\n  " + string.Join("\n  ", errors)
        );

      Console.Error.WriteLine($"[InteractionRollup] loaded {golds.Count} interaction golds from {goldsDir}");
      return golds;
    };

  private static void Validate(string fname, JsonObject g, List<string> errors)
  {
    var gid = g["id"]?.GetValue<string>() ?? fname;

    foreach (var k in new[] { "id", "unit", "cards", "ports", "edges", "declares" })
      if (g[k] is null)
        errors.Add($"{gid}: missing required key '{k}'");

    var unit = g["unit"]?.GetValue<string>();
    if (unit is not ("single-card" or "pairwise" or "combo"))
      errors.Add($"{gid}: unit must be single-card|pairwise|combo, got {unit ?? "null"}");

    // Port ids unique per card; build the resolvable "Card.Id" set.
    var ports = new HashSet<string>(StringComparer.Ordinal);
    if (g["ports"] is JsonObject portsObj)
    {
      foreach (var cardKv in portsObj)
      {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (cardKv.Value is not JsonArray plist)
          continue;
        foreach (var pn in plist)
        {
          if (pn is not JsonObject p)
            continue;
          var pid = p["id"]?.GetValue<string>();
          if (pid is not null && !seen.Add(pid))
            errors.Add($"{gid}: duplicate port id {cardKv.Key}.{pid}");
          ports.Add($"{cardKv.Key}.{pid}");
          foreach (var req in new[] { "side", "kind", "stem", "attrs" })
            if (p[req] is null)
              errors.Add($"{gid}: port {cardKv.Key}.{pid} missing '{req}'");
        }
      }
    }

    // Local declared rule ids.
    var localRules = new HashSet<string>(StringComparer.Ordinal);
    if (g["declares"] is JsonObject declares)
      foreach (var sec in Sections)
        if (declares[sec] is JsonArray arr)
          foreach (var rn in arr)
            if (rn is JsonObject r && r["id"]?.GetValue<string>() is string rid)
              localRules.Add(rid);

    // Edges resolve; non-structural mechanisms cite a rule.
    var eids = new HashSet<string>(StringComparer.Ordinal);
    if (g["edges"] is JsonArray edges)
    {
      foreach (var en in edges)
      {
        if (en is not JsonObject e)
          continue;
        var eid = e["id"]?.GetValue<string>();
        if (eid is not null && !eids.Add(eid))
          errors.Add($"{gid}: duplicate edge id {eid}");
        foreach (var end in new[] { "from", "to" })
        {
          var refv = e[end]?.GetValue<string>();
          if (refv is null || !ports.Contains(refv))
            errors.Add($"{gid}: edge {eid} {end}={refv ?? "null"} does not resolve to a declared port");
        }
        var mech = e["mechanism"]?.GetValue<string>();
        if (mech is not null && !Structural.Contains(mech))
        {
          var rule = e["rule"]?.GetValue<string>();
          if (string.IsNullOrEmpty(rule))
            errors.Add($"{gid}: edge {eid} mechanism={mech} must cite a 'rule'");
          // Cross-gold ("external") rule resolution is checked against the global union downstream.
        }
      }
    }
  }
}
