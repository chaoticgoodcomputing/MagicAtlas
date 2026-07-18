namespace MagicAST.Tests.Tests.InteractionRollup;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// Contract GATE for the InteractionRollup topology pipeline (ADR-0003 §8), landed as part of the
/// 2026-07-18 topology-rollup hardening pass. Two responsibilities:
///
/// <para><b>Part A — regeneration contract.</b> <see cref="TopologyStep"/> is a pure
/// <c>Func&lt;(golds, scaffold), (lean, cited)&gt;</c> — exactly like <c>CardPortsStep</c> in
/// <c>CardAtlasContractTests</c>. This fixture calls it directly on the COMMITTED golds + scaffold and
/// asserts the result is structurally identical to the committed
/// <c>Fixtures/Interactions/rollup/port-topology{,.cited}.json</c>. Any drift between the golds/scaffold
/// source-of-truth and the checked-in generated artifact fails the build — previously this only surfaced
/// via a human running <c>dotnet run -- --flow InteractionRollup</c> and eyeballing <c>git diff</c>.</para>
///
/// <para><b>Part B — gold assertion execution.</b> Every gold's <c>assertions[]</c> array carries
/// machine-checkable claims (see <c>Fixtures/Interactions/golds/README.md</c>). Two claim shapes are
/// cheap to execute against the regenerated topology right now — <c>stem.&lt;S&gt;.witnessed</c> and
/// <c>corroborates_hole.&lt;H&gt;</c> — this is the check that would have caught the
/// <c>non-play-zone-move</c> hole-resolution bug the instant <c>archaeomancer.json</c> landed. Any other
/// claim shape is SKIPPED (additive grammar, not a rewrite of assertion execution).</para>
///
/// <para><b>R2/R3 negative-path proofs.</b> A handful of tests construct malformed scaffolds in-memory
/// (never touching the committed fixture) and assert <see cref="TopologyStep"/> throws — proving the
/// hole-shape gate (R2) and the axis-constraint validator (R3) actually fire, not just that they compile.</para>
/// </summary>
[TestFixture]
public class TopologyRollupContractTests
{
  private static readonly string GoldsDir = Path.Combine(
    TestContext.CurrentContext.TestDirectory,
    "Fixtures",
    "Interactions",
    "golds"
  );

  private static readonly string ScaffoldPath = Path.Combine(
    TestContext.CurrentContext.TestDirectory,
    "Fixtures",
    "Interactions",
    "topology-scaffold.json"
  );

  private static readonly string CitedRollupPath = Path.Combine(
    TestContext.CurrentContext.TestDirectory,
    "Fixtures",
    "Interactions",
    "rollup",
    "port-topology.cited.json"
  );

  private static readonly string LeanRollupPath = Path.Combine(
    TestContext.CurrentContext.TestDirectory,
    "Fixtures",
    "Interactions",
    "rollup",
    "port-topology.json"
  );

  // Loaded via the SAME source steps InteractionRollupFlow wires up — not a reimplementation of the
  // loader's structural validation.
  private static readonly List<JsonNode> GoldList = LoadInteractionGoldsStep.Create(GoldsDir)().ToList();
  private static readonly JsonNode Scaffold = LoadTopologyScaffoldStep.Create(ScaffoldPath)();

  private static readonly (PortTopology Lean, PortTopology Cited) Regenerated = TopologyStep.Create()(
    (GoldList, Scaffold)
  );

  // ── Part A — regeneration contract ──────────────────────────────────────────────────────────────

  [Test]
  public void Regenerated_cited_topology_matches_the_committed_artifact()
  {
    AssertTopologyMatchesCommitted(Regenerated.Cited, CitedRollupPath, "port-topology.cited.json");
  }

  [Test]
  public void Regenerated_lean_topology_matches_the_committed_artifact()
  {
    AssertTopologyMatchesCommitted(Regenerated.Lean, LeanRollupPath, "port-topology.json");
  }

  private static void AssertTopologyMatchesCommitted(PortTopology topology, string committedPath, string label)
  {
    var expected = ToJson(topology);
    var committed = JsonNode.Parse(File.ReadAllText(committedPath))!.AsObject();
    var allKeys = expected
      .Select(kv => kv.Key)
      .Union(committed.Select(kv => kv.Key))
      .OrderBy(k => k, StringComparer.Ordinal);

    var diffs = new List<string>();
    foreach (var key in allKeys)
    {
      var e = expected.TryGetPropertyValue(key, out var ev) ? ev : null;
      var c = committed.TryGetPropertyValue(key, out var cv) ? cv : null;
      if (!JsonNode.DeepEquals(e, c))
        diffs.Add(key);
    }

    Assert.That(
      diffs,
      Is.Empty,
      $"regenerated {label} diverges from the committed artifact in top-level section(s): "
        + $"{string.Join(", ", diffs)} — regenerate via `dotnet run -- --flow InteractionRollup` "
        + "(from tests/magic-ast-tests) and `git diff` the rollup artifacts to see what moved."
    );
  }

  // ── Part B — machine-checkable gold assertions ──────────────────────────────────────────────────

  // "stem.<S>.witnessed" — S is a kebab-stem shape (may contain colons).
  private static readonly Regex StemWitnessedClaim = new(
    @"^stem\.(?<stem>[a-zA-Z0-9:_-]+)\.witnessed$",
    RegexOptions.Compiled
  );

  // "corroborates_hole.<H>" — H is the hole's scaffold key.
  private static readonly Regex CorroboratesHoleClaim = new(
    @"^corroborates_hole\.(?<hole>[a-zA-Z0-9_-]+)$",
    RegexOptions.Compiled
  );

  [Test]
  public void Every_recognized_gold_assertion_holds_against_the_regenerated_topology()
  {
    var topology = Regenerated.Cited; // witnesses are only populated in the cited half
    var errors = new List<string>();
    var recognized = 0;

    foreach (var gn in GoldList)
    {
      var g = gn.AsObject();
      var gid = g["id"]!.GetValue<string>();
      if (g["assertions"] is not JsonArray assertions)
        continue;

      foreach (var an in assertions)
      {
        var claim = an?["claim"]?.GetValue<string>();
        if (claim is null)
          continue;

        var stemMatch = StemWitnessedClaim.Match(claim);
        if (stemMatch.Success)
        {
          recognized++;
          CheckStemWitnessed(topology, gid, claim, stemMatch.Groups["stem"].Value, errors);
          continue;
        }

        var holeMatch = CorroboratesHoleClaim.Match(claim);
        if (holeMatch.Success)
        {
          recognized++;
          CheckHoleCorroborated(topology, gid, claim, holeMatch.Groups["hole"].Value, errors);
          continue;
        }

        // Any other claim shape (loop_tier ==, edge.*, no_loop, R1.from ==, …) is not yet executable —
        // SKIP it (additive grammar; see golds/README.md "Stage 3 shadow mode").
      }
    }

    // Sanity: the two claim shapes ARE present in the corpus (else this test would vacuously pass and
    // silently stop testing anything the moment every gold using them was deleted/renamed).
    Assert.That(recognized, Is.GreaterThan(0), "expected at least one stem.*.witnessed/corroborates_hole.* claim in the golds");

    Assert.That(errors, Is.Empty, "gold assertion(s) failed against the regenerated topology:\n  " + string.Join("\n  ", errors));
  }

  private static void CheckStemWitnessed(
    PortTopology topology,
    string goldId,
    string claim,
    string stem,
    List<string> errors
  )
  {
    if (!topology.Stems.TryGetValue(stem, out var entry))
    {
      errors.Add($"{goldId}: claim '{claim}' — stem '{stem}' is absent from the regenerated topology");
      return;
    }
    if (entry.Status != "witnessed")
    {
      errors.Add($"{goldId}: claim '{claim}' — stem '{stem}' status is '{entry.Status}', expected 'witnessed'");
      return;
    }
    if (entry.Witnesses is null || !entry.Witnesses.Contains(goldId))
      errors.Add($"{goldId}: claim '{claim}' — stem '{stem}' witnesses do not include this gold");
  }

  private static void CheckHoleCorroborated(
    PortTopology topology,
    string goldId,
    string claim,
    string hole,
    List<string> errors
  )
  {
    if (!topology.Holes.TryGetValue(hole, out var entry))
    {
      errors.Add($"{goldId}: claim '{claim}' — hole '{hole}' is absent from the regenerated topology");
      return;
    }
    if (entry.Status != "witnessed")
    {
      errors.Add($"{goldId}: claim '{claim}' — hole '{hole}' status is '{entry.Status}', expected 'witnessed'");
      return;
    }
    if (entry.Witnesses is null || !entry.Witnesses.Contains(goldId))
      errors.Add($"{goldId}: claim '{claim}' — hole '{hole}' witnesses do not include this gold");
  }

  // ── R2 negative-path proof: the hole proposed_stem shape gate ───────────────────────────────────

  [Test]
  public void A_malformed_hole_proposed_stem_throws_at_construction_instead_of_sitting_sought_forever()
  {
    // This is the EXACT bug class this session found by hand: a free-text placeholder that can never
    // match a real projected stem name. Reproduced in-memory — never touches the committed scaffold.
    var scaffold = JsonNode.Parse(
      """
      {
        "holes": {
          "bogus-hole": {
            "priority": 1,
            "kind": "EVENT",
            "proposed_stem": "zonechange (neither removal nor deployment)"
          }
        }
      }
      """
    )!;

    var ex = Assert.Throws<InvalidOperationException>(
      () => TopologyStep.Create()((Enumerable.Empty<JsonNode>(), scaffold))
    );
    Assert.That(ex!.Message, Does.Contain("bogus-hole"));
    Assert.That(ex.Message, Does.Contain("zonechange (neither removal nor deployment)"));
  }

  [Test]
  public void A_well_shaped_hole_proposed_stem_does_not_throw()
  {
    var scaffold = JsonNode.Parse(
      """
      {
        "holes": {
          "some-hole": {
            "priority": 1,
            "kind": "EVENT",
            "proposed_stem": "modification:restriction"
          }
        }
      }
      """
    )!;

    Assert.DoesNotThrow(() => TopologyStep.Create()((Enumerable.Empty<JsonNode>(), scaffold)));
  }

  // ── R3 negative-path proof: reverting a fix reintroduces the drift and fails loud ───────────────

  [Test]
  public void Reverting_the_color_enum_fix_makes_the_axis_validator_fail_loud()
  {
    var mutated = Scaffold.DeepClone();
    mutated["attribute_axes"]!["color"]!["enum"] = JsonNode.Parse(
      """["W", "U", "B", "R", "G", "C", "any"]"""
    );

    var ex = Assert.Throws<InvalidOperationException>(
      () => TopologyStep.Create()((GoldList, mutated))
    );
    Assert.That(ex!.Message, Does.Contain("'color'"));
  }

  [Test]
  public void Reverting_the_counter_type_enum_fix_makes_the_axis_validator_fail_loud()
  {
    var mutated = Scaffold.DeepClone();
    mutated["attribute_axes"]!["counter-type"]!["enum"] = JsonNode.Parse(
      """["+1/+1", "loyalty", "charge", "energy", "experience", "poison"]"""
    );

    var ex = Assert.Throws<InvalidOperationException>(
      () => TopologyStep.Create()((GoldList, mutated))
    );
    Assert.That(ex!.Message, Does.Contain("'counter-type'"));
  }

  [Test]
  public void Reverting_the_from_to_licensed_by_fix_makes_the_axis_validator_fail_loud()
  {
    var mutated = Scaffold.DeepClone();
    mutated["attribute_axes"]!["from"]!["licensed_by"] = JsonNode.Parse("""["removal:*", "deployment:*"]""");
    mutated["attribute_axes"]!["to"]!["licensed_by"] = JsonNode.Parse("""["removal:*", "deployment:*"]""");

    var ex = Assert.Throws<InvalidOperationException>(
      () => TopologyStep.Create()((GoldList, mutated))
    );
    Assert.That(ex!.Message, Does.Contain("'from'").Or.Contain("'to'"));
  }

  // ── JSON mapping: PortTopology → JsonNode, mirroring the [SerializedLabel] wire shape exactly ────

  private static JsonObject ToJson(PortTopology t)
  {
    var root = new JsonObject
    {
      ["$generated"] = t.Generated,
      ["$golds"] = ToArray(t.Golds),
      ["kinds"] = ToStringMap(t.Kinds),
      ["supergroups"] = ToObjectMap(
        t.Supergroups,
        s => new JsonObject { ["kind_view"] = s.KindView, ["def"] = s.Def }
      ),
      ["event_verbs"] = ToObjectMap(
        t.EventVerbs,
        e => new JsonObject { ["kind"] = e.Kind, ["def"] = e.Def }
      ),
      ["stems"] = ToObjectMap(t.Stems, StemToJson),
      ["attribute_axes"] = ToObjectMap(t.AttributeAxes, AxisToJson),
      ["aliases"] = ToStringMap(t.Aliases),
      ["holes"] = ToObjectMap(t.Holes, HoleToJson),
    };
    return root;
  }

  private static JsonObject StemToJson(StemEntry s)
  {
    var o = new JsonObject { ["kind"] = s.Kind };
    if (s.Parent is not null)
      o["parent"] = s.Parent;
    o["status"] = s.Status;
    o["attrs"] = ToArray(s.Attrs);
    if (s.Unpredicted is bool unpredicted)
      o["unpredicted"] = unpredicted;
    if (s.Witnesses is { Count: > 0 } witnesses)
      o["witnesses"] = ToArray(witnesses);
    return o;
  }

  private static JsonObject AxisToJson(AxisEntry a)
  {
    var o = new JsonObject
    {
      ["stems"] = ToArray(a.Stems),
      ["values_seen"] = ToArray(a.ValuesSeen),
      ["carries_provenance_or_polarity"] = a.CarriesProvenanceOrPolarity,
    };
    if (a.LicensedBy is { Count: > 0 } licensedBy)
      o["licensed_by"] = ToArray(licensedBy);
    if (a.Lattice is not null)
      o["lattice"] = a.Lattice;
    if (a.Enum is { Count: > 0 } enumValues)
      o["enum"] = ToArray(enumValues);
    if (a.Bindable is { Count: > 0 } bindable)
      o["bindable"] = ToArray(bindable);
    if (a.Kind is not null)
      o["kind"] = a.Kind;
    if (a.Note is not null)
      o["note"] = a.Note;
    return o;
  }

  private static JsonObject HoleToJson(HoleEntry h)
  {
    var o = new JsonObject
    {
      ["priority"] = h.Priority,
      ["kind"] = h.Kind,
      ["proposed_stem"] = h.ProposedStem,
    };
    if (h.Attrs is { Count: > 0 } attrs)
      o["attrs"] = ToArray(attrs);
    if (h.Slang is { Count: > 0 } slang)
      o["slang"] = ToArray(slang);
    if (h.Note is not null)
      o["note"] = h.Note;
    o["status"] = h.Status;
    if (h.Witnesses is { Count: > 0 } witnesses)
      o["witnesses"] = ToArray(witnesses);
    return o;
  }

  private static JsonArray ToArray(IEnumerable<string> xs)
  {
    var arr = new JsonArray();
    foreach (var x in xs)
      arr.Add(JsonValue.Create(x));
    return arr;
  }

  private static JsonObject ToStringMap(IReadOnlyDictionary<string, string> d)
  {
    var o = new JsonObject();
    foreach (var kv in d)
      o[kv.Key] = kv.Value;
    return o;
  }

  private static JsonObject ToObjectMap<T>(IReadOnlyDictionary<string, T> d, Func<T, JsonObject> map)
  {
    var o = new JsonObject();
    foreach (var kv in d)
      o[kv.Key] = map(kv.Value);
    return o;
  }
}
