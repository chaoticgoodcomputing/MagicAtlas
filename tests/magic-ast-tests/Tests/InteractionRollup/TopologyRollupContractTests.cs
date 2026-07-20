namespace MagicAST.Tests.Tests.InteractionRollup;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.Interaction;
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
/// <para><b>Part A is now the SECTION-LEVEL diagnosis, not the gate of record.</b> ADR-0004 §3 (issue #24)
/// makes the rollup's committed exception carry a full byte-identity regeneration gate —
/// <see cref="RollupRegenerationGateTests"/> — which runs the real flow against a busted cache and covers
/// all four artifacts, including the two <c>port-interactions</c> files this fixture never touched. Part A
/// survives because its per-section report ("diverges in section: stems") localizes a failure that the byte
/// gate can only report as an offset. It is deliberately NOT the byte gate: the <c>ToJson</c> mirror below
/// is a hand-written restatement of the wire shape, so it can only ever prove the step and the mirror agree
/// — which is exactly why the real serializer had to be brought into the loop next door.</para>
///
/// <para><b>Part B — gold assertion execution.</b> Every gold's <c>assertions[]</c> array carries
/// machine-checkable claims (see <c>Fixtures/Interactions/golds/README.md</c>). Two claim shapes are
/// executable against the regenerated topology right now — <c>stem.&lt;S&gt;.witnessed</c> and (ADR-0004 §1)
/// <c>no_arm[P]</c>. Any other claim shape is SKIPPED (additive grammar, not a rewrite of assertion
/// execution) — EXCEPT the RETIRED <c>corroborates_hole.&lt;H&gt;</c> shape, which is failed explicitly so a
/// claim against the deleted hole registry can never rot into a silent skip (ADR-0004 §7 / issue #26).</para>
///
/// <para><b>Asserted absence (ADR-0004 §1).</b> <c>no_arm[P]</c> is the sibling of the established
/// <c>no_loop</c> claim: a domain judgment ("this port connects to nothing") carried as Evidence with an
/// EXECUTABLE justification instead of prose in a whitelist. It is evaluated against
/// <see cref="PortFlowMatcher.SelectArm"/> over the CURRENT witnessed stem universe
/// (<see cref="FlowProbes"/>), so it strengthens as the taxonomy accretes — and a gold may go red because
/// somebody armed a stem, with nobody touching the card. That is a hard build failure, judge-resolved.</para>
///
/// <para><b>Witness-derivation invariants (ADR-0004 §7 / issue #26).</b> The scaffold's declared half —
/// <c>stems_representative</c>, <c>aliases</c>, <c>attribute_axes</c>, <c>holes</c> — is deleted, so
/// <c>port-topology.json</c> is 100% witness-derived. Three stateless invariants pin that: the scaffold
/// carries none of those keys; every stem in the artifact is <c>witnessed</c> and cites at least one gold;
/// and adding scaffold sections back changes nothing (the step ignores them). The former R2/R3 negative-path
/// proofs are gone with the machinery they proved — a hole-shape gate and an axis-constraint validator
/// cannot fire when there are no declared holes and no declared axis constraints.</para>
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

  // RETIRED (ADR-0004 §7 / issue #26). Each of these names something the topology no longer stores: the
  // `holes{}` registry (corroborates_hole / burns_down_hole / hole.*) or the per-stem `unpredicted` flag,
  // which asserted a stem was absent from a declared prediction set that no longer exists. Matched only to
  // FAIL. Without this they would fall into the "unrecognized claim shape" SKIP bucket and rot silently —
  // which is precisely how a claim about a deleted field survives as documentation of a fiction.
  private static readonly Regex RetiredClaimShape = new(
    @"^(corroborates_hole\.|burns_down_hole\.|hole\.)|\.unpredicted",
    RegexOptions.Compiled
  );

  // "no_arm[<P>]" — ADR-0004 §1. P names a port by the gold-local `ports[card][].id` (the same identity
  // convention `edges[].from/to` use, minus the card qualifier, which is only needed when a gold declares
  // the same id on two cards — accepted here as "Card.Id" too).
  private static readonly Regex NoArmClaim = new(
    @"^no_arm\[(?<port>[^\]]+)\]$",
    RegexOptions.Compiled
  );

  [Test]
  public void Every_recognized_gold_assertion_holds_against_the_regenerated_topology()
  {
    var topology = Regenerated.Cited; // witnesses are only populated in the cited half
    var errors = new List<string>();
    var recognized = 0;
    var noArmClaims = 0;

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

        if (RetiredClaimShape.IsMatch(claim))
        {
          errors.Add(
            $"{gid}: claim '{claim}' uses a RETIRED shape — ADR-0004 §7 (issue #26) deleted the topology's "
              + "holes{} registry and the per-stem `unpredicted` flag, so it resolves against nothing. A "
              + "hole burn-down and a never-predicted stem are now the SAME claim: "
              + "`stem.<S>.witnessed`. Restate it that way rather than reviving the field."
          );
          continue;
        }

        var noArmMatch = NoArmClaim.Match(claim);
        if (noArmMatch.Success)
        {
          recognized++;
          noArmClaims++;
          CheckNoArm(topology, g, gid, claim, noArmMatch.Groups["port"].Value, errors);
          continue;
        }

        // Any other claim shape (loop_tier ==, edge.*, no_loop, R1.from ==, …) is not yet executable —
        // SKIP it (additive grammar; see golds/README.md "Stage 3 shadow mode").
      }
    }

    // Sanity: the executable claim shapes ARE present in the corpus (else this test would vacuously pass
    // and silently stop testing anything the moment every gold using them was deleted/renamed).
    Assert.That(recognized, Is.GreaterThan(0), "expected at least one stem.*.witnessed claim in the golds");

    // ADR-0004 §1: the no_arm machinery must have a live user. An unexercised assertion runner is the
    // same vacuity failure one level up — it would pass forever while testing nothing.
    Assert.That(
      noArmClaims,
      Is.GreaterThan(0),
      "expected at least one no_arm[P] claim in the golds — the asserted-absence machinery is shipped "
        + "unexercised otherwise (ADR-0004 §1)"
    );

    Assert.That(errors, Is.Empty, "gold assertion(s) failed against the regenerated topology:\n  " + string.Join("\n  ", errors));
  }

  /// <summary>
  /// ADR-0004 §1 — execute a <c>no_arm[P]</c> claim. P is named by its gold-local port id; the port's
  /// declared <c>side</c>/<c>stem</c>/<c>attrs</c> ARE its identity (the ADR-0003 structure canonical form),
  /// so the claim reads: <em>were this port structured exactly as the gold declares it, no flow arm would
  /// connect it to anything the current taxonomy knows about.</em>
  ///
  /// <para>Asserted against <see cref="PortFlowMatcher.SelectArm"/> over the probe universe
  /// (<see cref="FlowProbes"/>), never against a materialized edge set — a single-card gold has no partner,
  /// so "zero edges" would be vacuously true and would keep passing after someone armed the port.</para>
  ///
  /// <para><b>Triage doctrine (ADR-0004 Consequences):</b> a firing here is a HARD BUILD FAILURE, resolved
  /// by the judge — either the new arm is correct (amend or delete the gold, judge-gated) or the arm is
  /// wrong (fix it). It is never resolved by weakening this assertion, and never deferred to a report.</para>
  /// </summary>
  private static void CheckNoArm(
    PortTopology topology,
    JsonObject gold,
    string goldId,
    string claim,
    string portRef,
    List<string> errors
  )
  {
    // ── resolve P (a non-resolving port is a FAILURE, never a silent skip — that is the vacuity trap) ──
    var matches = new List<(string Card, JsonObject Port)>();
    foreach (var cardKv in gold["ports"]!.AsObject())
    {
      if (cardKv.Value is not JsonArray plist)
        continue;
      foreach (var pn in plist)
      {
        if (pn is not JsonObject p)
          continue;
        var pid = p["id"]?.GetValue<string>();
        if (pid == portRef || $"{cardKv.Key}.{pid}" == portRef)
          matches.Add((cardKv.Key, p));
      }
    }

    if (matches.Count == 0)
    {
      errors.Add($"{goldId}: claim '{claim}' — port '{portRef}' does not resolve to a declared port");
      return;
    }
    if (matches.Count > 1)
    {
      errors.Add(
        $"{goldId}: claim '{claim}' — port '{portRef}' is ambiguous across {matches.Count} cards; "
          + "qualify it as 'Card.Id'"
      );
      return;
    }

    var (_, port) = matches[0];
    var sideText = port["side"]!.GetValue<string>();
    var side = sideText switch
    {
      "emit" => PortSide.Emit,
      "consume" => PortSide.Consume,
      _ => (PortSide?)null,
    };
    if (side is null)
    {
      // `intercept` has no SelectArm role at all (the matcher is emit×consume), so an absence claim over it
      // would be vacuous by construction. Fail loudly rather than pass for free.
      errors.Add(
        $"{goldId}: claim '{claim}' — port side '{sideText}' is not assertable; no_arm is evaluated over "
          + "SelectArm(emit, consume), so P must be side=emit or side=consume"
      );
      return;
    }

    var asserted = PortStructure.Of(
      side.Value,
      port["stem"]!.GetValue<string>(),
      GoldAttrs(port["attrs"]).ToArray()
    );

    // ── the counterparty side, and the non-vacuity guards on it ──
    var otherSide = side.Value == PortSide.Emit ? PortSide.Consume : PortSide.Emit;
    var witnessed = FlowProbes.WitnessedStems(topology);
    var probes = FlowProbes.For(topology, otherSide);
    var liveOnSide = FlowProbes.Live.Count(p => p.Structure.Side == otherSide);

    if (witnessed.Count == 0 || liveOnSide == 0 || probes.Count == 0)
    {
      errors.Add(
        $"{goldId}: claim '{claim}' — the probe universe is degenerate (witnessed stems {witnessed.Count}, "
          + $"live {otherSide.ToString().ToLowerInvariant()} structures {liveOnSide}, probes {probes.Count}); "
          + "an absence claim evaluated against an empty universe is vacuous"
      );
      return;
    }

    // ── the assertion, over BOTH vocabularies the port can be spelled in ──
    // The gold's canonical stem is the ADR-0003 spelling. An UNSTRUCTURED port additionally records the
    // coarse label the engine actually emits (`coarse_label`), because that is the spelling a future family
    // would arm — a probe keyed only on the canonical stem would sail past the likeliest wrong-making
    // change (interaction-judge, 2026-07-20).
    var spellings = new List<PortStructure> { asserted };
    var coarseLabel = port["coarse_label"]?.GetValue<string>();
    if (coarseLabel is not null)
    {
      // The recorded label must still be one the engine emits UNSTRUCTURED. If it is gone, either the label
      // was renamed or (the case that matters) a family now recognizes it — in both cases the judgment must
      // be re-derived, not silently carried.
      var key = FlowProbes.CoarseKey(side.Value, coarseLabel);
      if (!FlowProbes.UnstructuredLabels.Contains(key))
      {
        errors.Add(
          $"{goldId}: claim '{claim}' — the recorded coarse label '{coarseLabel}' is no longer projected "
            + $"unstructured on the {sideText} side anywhere in the hand-parsed corpus. Either it was "
            + "renamed, or an IPortFamily now structures it — which is the first step to arming it. "
            + "ADR-0004: re-derive the judgment (judge-gated); do not update the label to make this pass."
        );
        return;
      }
      spellings.Add(PortStructure.Of(side.Value, FlowProbes.CoarseStem(side.Value, coarseLabel)));
    }

    foreach (var spelling in spellings)
    {
      var hits = FlowProbes.ArmsFor(spelling, probes);
      if (hits.Count > 0)
        errors.Add(
          $"{goldId}: claim '{claim}' FALSIFIED — {spelling.Canonical()} now selects "
            + string.Join(
              ", ",
              hits.Select(h => $"{h.Arm} against {h.Counterparty}")
            )
            + ". ADR-0004: this is a hard failure resolved by the interaction-judge — either the arm is "
            + "correct (amend/delete this gold) or the arm is wrong (fix the matcher). Never weaken the claim."
        );
    }
  }

  /// <summary>
  /// A gold's <c>attrs</c> in the spelling the MATCHER uses. Values may be a bare scalar or the
  /// provenance/polarity object form (<c>{"value": …, "provenance": "derived"}</c>) — provenance does not
  /// participate in arm selection, so only the value is carried. Booleans render <c>true</c>/<c>false</c>
  /// (the engine's own spelling, e.g. <c>emit.Attr("token") == "true"</c>) — deliberately NOT the rollup's
  /// Python-style <c>True</c>/<c>False</c>, which is a serialization detail of the topology artifact.
  /// </summary>
  private static IEnumerable<(string Key, string Value)> GoldAttrs(JsonNode? attrs)
  {
    if (attrs is not JsonObject obj)
      yield break;
    foreach (var kv in obj)
    {
      var v = kv.Value is JsonObject o ? o["value"] : kv.Value;
      if (v is null)
        continue;
      var text = v.GetValueKind() switch
      {
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.String => v.GetValue<string>(),
        _ => v.ToJsonString(),
      };
      yield return (kv.Key, text);
    }
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

  // ── ADR-0004 §7 / issue #26: the topology is 100% witness-derived ───────────────────────────────

  /// <summary>
  /// The scaffold no longer carries a DECLARED half. This is the check that makes "no hand-typed
  /// <c>status</c> can survive" structural rather than a claim: the four deleted sections were the only
  /// places a human could type a stem, an axis constraint, an alias or a hole <c>status</c> into a file the
  /// generator copies into <c>port-topology.json</c>. Re-adding any of them fails here.
  /// </summary>
  [Test]
  public void The_scaffold_carries_no_declared_half()
  {
    var deleted = new[] { "stems_representative", "aliases", "attribute_axes", "holes" };
    var present = deleted.Where(k => Scaffold[k] is not null).ToList();
    Assert.That(
      present,
      Is.Empty,
      "topology-scaffold.json re-declared section(s) deleted by ADR-0004 §7 (issue #26): "
        + string.Join(", ", present)
        + ". stems/attribute_axes are derived from the golds, aliases and holes no longer exist in the "
        + "artifact at all. A declared half is the drift surface that failed twice — do not reintroduce it."
    );
  }

  /// <summary>
  /// Every stem in the artifact is <c>witnessed</c> and names at least one witnessing gold. The status
  /// field is retained only because the executable <c>stem.&lt;S&gt;.witnessed</c> claims read it; this
  /// pins it as a derived constant so it cannot become a hand-set discriminator again.
  /// </summary>
  [Test]
  public void Every_stem_in_the_topology_is_witnessed_by_at_least_one_gold()
  {
    var offenders = Regenerated
      .Cited.Stems.Where(kv => kv.Value.Status != "witnessed" || kv.Value.Witnesses is not { Count: > 0 })
      .Select(kv => $"{kv.Key} (status={kv.Value.Status}, witnesses={kv.Value.Witnesses?.Count ?? 0})")
      .ToList();

    Assert.That(
      offenders,
      Is.Empty,
      "port-topology.json must be 100% witness-derived (ADR-0004 §7) — offending stem(s): "
        + string.Join(", ", offenders)
    );
  }

  /// <summary>
  /// The deletion is enforced in the GENERATOR, not merely in the fixture: feeding the step a scaffold that
  /// still carries all four declared sections produces byte-identical output. Without this, someone could
  /// re-add the sections and the step would silently start merging them again.
  /// </summary>
  [Test]
  public void Reintroducing_the_deleted_scaffold_sections_changes_nothing()
  {
    var resurrected = Scaffold.DeepClone();
    resurrected["stems_representative"] = JsonNode.Parse(
      """{ "a-stem-no-gold-projects": { "kind": "EVENT" } }"""
    );
    resurrected["aliases"] = JsonNode.Parse("""{ "fodder": "creature[pt=1/1]" }""");
    resurrected["attribute_axes"] = JsonNode.Parse(
      """{ "color": { "enum": ["nonsense"], "licensed_by": ["nothing:*"] }, "an-axis-no-gold-carries": {} }"""
    );
    resurrected["holes"] = JsonNode.Parse(
      """{ "a-hole": { "priority": 1, "kind": "EVENT", "proposed_stem": "never:witnessed" } }"""
    );

    var (lean, cited) = TopologyStep.Create()((GoldList, resurrected));
    Assert.Multiple(() =>
    {
      Assert.That(JsonNode.DeepEquals(ToJson(lean), ToJson(Regenerated.Lean)), Is.True, "lean topology changed");
      Assert.That(JsonNode.DeepEquals(ToJson(cited), ToJson(Regenerated.Cited)), Is.True, "cited topology changed");
    });
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
    if (s.Witnesses is { Count: > 0 } witnesses)
      o["witnesses"] = ToArray(witnesses);
    return o;
  }

  private static JsonObject AxisToJson(AxisEntry a) =>
    new()
    {
      ["stems"] = ToArray(a.Stems),
      ["values_seen"] = ToArray(a.ValuesSeen),
      ["carries_provenance_or_polarity"] = a.CarriesProvenanceOrPolarity,
    };

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
