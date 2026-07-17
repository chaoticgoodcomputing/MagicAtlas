using System.Text.Json.Nodes;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.SpanWitness.Steps;

/// <summary>
/// The span-witness error-check (<see cref="SpanWitnessReport"/>). Treats each parsed port's span as a
/// WITNESS — the exact oracle-text it claims — and flags ports whose claimed text lacks the anchor word
/// their label asserts. Buckets: <b>derived</b> (a created token's affordance, span borrowed from its
/// creator — excluded), <b>misaligned</b> (span past the stored text = the double-faced-card class), and
/// <b>semantic</b> (text present, anchor absent — the actionable suspect). Each semantic suspect is routed
/// to the golds that witness its ADR-3 stem (the cited topology's <c>stems[stem].witnesses</c>), so the
/// loop knows which interaction to refine. Reads the committed cited topology directly; degrades to no
/// routing (empty witnesses) if it is absent.
/// </summary>
[FlowthruStep]
public static class SpanWitnessStep
{
  private const string Note =
    "Span-witness error-check (mast-loop Error-check track). A port's SourceSpan is a witness: the exact "
    + "oracle chars it claims. A 'semantic' outlier — the span has text but lacks the label's anchor word "
    + "— is a suspect: a false-positive port OR a span mis-attribution. 'witnessGolds' routes each to the "
    + "interaction golds that witness its stem (refine the gold, or the parser slice that mints the port). "
    + "Anchor vocabulary is deliberately conservative (keyword mechanics like firebending/modular/embalm "
    + "are aliased in) — extend it as new keyword→effect mappings surface, never suppress a real suspect.";

  /// <summary>The anchor word(s) a port's own oracle text must contain, by label head. Empty = no checkable
  /// anchor (skip — an unparsed catch-all or a mana-cost-derived cost with no oracle text). Keyword-mechanic
  /// aliases (firebending→mana, modular→dies, embalm→create) are folded in so a correct keyword port is not
  /// a false alarm.</summary>
  private static string[]? AnchorsFor(string label)
  {
    var seg = label.Split(':');
    var role = seg[0];
    var kind = seg.Length > 1 ? seg[1] : "";
    if (label.StartsWith("evasion:flying", StringComparison.Ordinal))
      return ["flying"];
    return (role, kind) switch
    {
      ("sac", _) => ["sacrific"],
      ("etb", _) => ["enters"],
      ("ltb", _) => ["dies", "graveyard", "leaves", "put into", "modular"], // modular's dies-trigger
      ("trigger", "damage") => ["damage"],
      ("emit", "damage") => ["damage", "deals"],
      ("emit", "token") => ["create", "embalm"], // embalm makes a token copy
      ("emit", "rolldice") or ("trigger", "rolldice") => ["roll"],
      ("trigger", "cast") => ["cast"],
      ("emit", "mana") => ["add", "firebending"], // firebending adds red mana on attack
      ("emit", "life") => ["life"],
      ("tap", _) => ["{t}", "tap"],
      _ => null,
    };
  }

  public static Func<
    (IEnumerable<CardPortRow> Ports, IEnumerable<MastCardInput> CardInputs),
    SpanWitnessReport
  > Create(string citedTopologyPath) =>
    inputs =>
    {
      var text = new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var ci in inputs.CardInputs)
        text.TryAdd(ci.Input.Name, ci.Input.OracleText ?? "");

      var ports = inputs.Ports.ToList();

      // Derived affordance spans: any non-token port whose span coincides with an emit:token span on the
      // same card inherited that clause (a Treasure's sac/mana/tap) — its text is the creator's, not its own.
      var tokenSpans = new Dictionary<string, HashSet<(int, int)>>(StringComparer.Ordinal);
      foreach (var p in ports)
        if (p.Label.StartsWith("emit:token", StringComparison.Ordinal) && p.Spans is { } ts)
          foreach (var s in ts)
            (tokenSpans.TryGetValue(p.Card, out var set) ? set : tokenSpans[p.Card] = new()).Add((s[0], s[1]));

      var witnesses = LoadStemWitnesses(citedTopologyPath);

      var checkedCount = 0;
      var derived = 0;
      var misaligned = 0;
      var outliers = new List<SpanOutlierRow>();

      foreach (var p in ports)
      {
        if (p.Tier is not ("Green" or "Amber") || p.Spans is not { Length: > 0 } spans)
          continue;
        var anchors = AnchorsFor(p.Label);
        if (anchors is null)
          continue;
        checkedCount++;

        if (
          p.Family != "token"
          && tokenSpans.TryGetValue(p.Card, out var tset)
          && spans.Any(s => tset.Contains((s[0], s[1])))
        )
        {
          derived++;
          continue;
        }

        var oracle = text.GetValueOrDefault(p.Card, "");
        var frag = string.Join(
          " ",
          spans.Select(s => s[0] >= 0 && s[1] <= oracle.Length && s[1] > s[0] ? oracle[s[0]..s[1]] : "")
        );
        var lower = frag.ToLowerInvariant();
        if (anchors.Any(a => lower.Contains(a, StringComparison.Ordinal)))
          continue;

        if (string.IsNullOrWhiteSpace(frag))
        {
          misaligned++;
          continue;
        }

        outliers.Add(
          new SpanOutlierRow
          {
            Card = p.Card,
            Label = p.Label,
            Family = p.Family,
            Stem = p.Stem,
            ExpectedAnchor = string.Join(" | ", anchors),
            ClaimedText = frag.Length > 120 ? frag[..120] : frag,
            WitnessGolds = WitnessesFor(p.Stem, witnesses),
          }
        );
      }

      // Rank: unwitnessed stems first (a suspect on a stem no gold covers is a QA flag AND an accretion
      // gap), then by stem, then by card — a stable, actionable order.
      var ranked = outliers
        .OrderBy(o => o.WitnessGolds.Count == 0 ? 0 : 1)
        .ThenBy(o => o.Stem ?? "~", StringComparer.Ordinal)
        .ThenBy(o => o.Card, StringComparer.Ordinal)
        .ToList();

      Console.Error.WriteLine(
        $"[SpanWitness] {checkedCount} checked · {derived} derived-excluded · {misaligned} misaligned(DFC) · "
          + $"{ranked.Count} semantic suspects ({ranked.Count(o => o.WitnessGolds.Count == 0)} on unwitnessed stems)"
      );

      return new SpanWitnessReport
      {
        GeneratedAt = "Flows/SpanWitness",
        Note = Note + (witnesses.Count == 0 ? " [cited topology absent — witness routing empty]" : ""),
        CheckedPorts = checkedCount,
        DerivedExcluded = derived,
        MisalignedDfc = misaligned,
        SemanticOutlierCount = ranked.Count,
        Outliers = ranked,
      };
    };

  /// <summary>The golds witnessing a port's ADR-3 stem: try the full stem, then the card-type leaf of a
  /// <c>supergroup:type</c> stem (<c>removal:creature</c> → <c>creature</c>). Empty when declared-only /
  /// event-verb (dice/damage carry no per-stem witness today) — surfaced as an uncovered stem.</summary>
  private static IReadOnlyList<string> WitnessesFor(
    string? stem,
    IReadOnlyDictionary<string, string[]> witnesses
  )
  {
    if (stem is null)
      return [];
    if (witnesses.TryGetValue(stem, out var direct))
      return direct;
    var leaf = stem.Contains(':', StringComparison.Ordinal) ? stem[(stem.LastIndexOf(':') + 1)..] : stem;
    return witnesses.TryGetValue(leaf, out var byLeaf) ? byLeaf : [];
  }

  /// <summary>Parse <c>stems[name].witnesses</c> from the committed cited topology; empty on absence
  /// (graceful degrade — the check still runs, only routing is blank).</summary>
  private static IReadOnlyDictionary<string, string[]> LoadStemWitnesses(string citedTopologyPath)
  {
    var map = new Dictionary<string, string[]>(StringComparer.Ordinal);
    if (!File.Exists(citedTopologyPath))
      return map;
    if (JsonNode.Parse(File.ReadAllText(citedTopologyPath))?["stems"] is not JsonObject stems)
      return map;
    foreach (var (name, info) in stems)
      if (info?["witnesses"] is JsonArray w)
        map[name] = w.Select(x => x!.ToString()).ToArray();
    return map;
  }
}
