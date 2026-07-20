namespace MagicAST.Tests.Tests.InteractionRollup;

using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// ADR-0004 §7 (issue #27) — the scaffold's <c>connectivity_predictions</c> P1–P6, promoted from prose in
/// <c>topology-scaffold.json</c> to <b>executable sweeps</b> over the witnessed graph.
///
/// <para><b>Why this fixture exists.</b> ADR-0003 Migration Stage 0a justified the scaffold as "falsifiable
/// connectivity predictions … the null hypotheses for the topology sweeps". ADR-0004 §7 measured that claim
/// and found the predictions <em>"exist only in the scaffold and are checked by nothing"</em> — unfalsifiable
/// in practice since Stage 0a. Each test below names the prediction it encodes and states, explicitly,
/// <b>what would falsify it</b>. The <c>connectivity_predictions</c> block is deleted from the scaffold in
/// the same change: the prediction now lives where it can go red.</para>
///
/// <para><b>A failing sweep is a RESULT, not a bug in the sweep</b> (ADR-0003: "a divergence either
/// falsifies the scaffold or flags a bad derivation"). The resolution is an interaction-judge verdict —
/// falsified scaffold hypothesis, or genuine topology gap. It is <em>never</em> resolved by softening a
/// predicate until it passes. Where a prediction was ruled falsified-as-stated, the ruling and its
/// counterexample are pinned executably here (see <see cref="P1_sac_outlet_reaches_all_three_consumer_rungs"/>
/// and <see cref="P2_token_maker_reaches_every_object_store_fodder_consumer"/>), so the corrected claim is
/// asserted AND the falsifying instance cannot silently change.</para>
///
/// <para><b>Vacuity.</b> Every sweep asserts its own population is non-empty and writes the count it
/// examined to the test output. A sweep whose candidate set came back empty is worthless — that failure mode
/// has bitten this milestone repeatedly, so it is an assertion here, not a convention.</para>
///
/// <para><b>The universe.</b> Two derived halves, exactly as <see cref="FlowProbes"/> defines them for
/// <c>no_arm</c>: the <b>gold-declared ports</b> (a port's <c>side</c>/<c>stem</c>/<c>attrs</c> ARE its
/// ADR-0003 structure) and the <b>live projection</b> (every distinct <see cref="PortStructure"/> the
/// engine's families project over the hand-parsed corpus). The standing dual-vocabulary fact applies: the
/// golds and the rollup say <c>damage-dealt</c>/<c>dice-rolled</c>/<c>combat-presence</c> where the engine
/// says <c>damage</c>/<c>dice</c>/<c>combat</c>, and the gold vocabulary for a sac-fodder consume omits the
/// <c>manner=sacrificed</c> facet the engine's own projection carries (P2's finding).</para>
/// </summary>
[TestFixture]
public class ConnectivityPredictionSweeps
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

  private static readonly List<JsonNode> GoldList = LoadInteractionGoldsStep.Create(GoldsDir)().ToList();
  private static readonly JsonNode Scaffold = LoadTopologyScaffoldStep.Create(ScaffoldPath)();

  private static readonly PortTopology Topology = TopologyStep.Create()((GoldList, Scaffold)).Item2;

  // ── the universe the sweeps quantify over ───────────────────────────────────────────────────────

  /// <summary>One member of the sweep universe: a structure plus where it came from (failure messages).</summary>
  private sealed record UPort(PortStructure Structure, string Origin)
  {
    public override string ToString() => $"{Structure.Canonical()} ({Origin})";
  }

  /// <summary>An edge a gold declares, with both endpoints resolved to their declared structures.</summary>
  private sealed record GEdge(
    string GoldId,
    string EdgeId,
    string Mechanism,
    string? Rule,
    string Tier,
    PortStructure From,
    PortStructure To,
    string FromRef,
    string ToRef,
    string FromCard,
    string ToCard
  )
  {
    public override string ToString() =>
      $"{GoldId}.{EdgeId} [{Mechanism}{(Rule is null ? "" : "/" + Rule)}] {Tier}: "
      + $"{From.Canonical()} → {To.Canonical()}";
  }

  private static PortSide? SideOf(string? s) =>
    s switch
    {
      "emit" => PortSide.Emit,
      "consume" => PortSide.Consume,
      _ => null, // `intercept` has no SelectArm role — the matcher is emit × consume.
    };

  /// <summary>A gold's <c>attrs</c> in the spelling the MATCHER uses (mirrors TopologyRollupContractTests).</summary>
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
        System.Text.Json.JsonValueKind.True => "true",
        System.Text.Json.JsonValueKind.False => "false",
        System.Text.Json.JsonValueKind.String => v.GetValue<string>(),
        _ => v.ToJsonString(),
      };
      yield return (kv.Key, text);
    }
  }

  private static readonly Lazy<IReadOnlyList<UPort>> GoldPortsLazy = new(() =>
  {
    var ports = new List<UPort>();
    foreach (var gn in GoldList)
    {
      var g = gn.AsObject();
      var gid = g["id"]!.GetValue<string>();
      foreach (var cardKv in g["ports"]!.AsObject())
      {
        if (cardKv.Value is not JsonArray plist)
          continue;
        foreach (var pn in plist)
        {
          if (pn is not JsonObject p)
            continue;
          var side = SideOf(p["side"]?.GetValue<string>());
          var stem = p["stem"]?.GetValue<string>();
          if (side is null || stem is null)
            continue;
          ports.Add(
            new UPort(
              PortStructure.Of(side.Value, stem, GoldAttrs(p["attrs"]).ToArray()),
              $"gold {gid}:{cardKv.Key}.{p["id"]}"
            )
          );
        }
      }
    }
    return ports;
  });

  /// <summary>Every side-bearing port any gold declares, as its ADR-0003 structure.</summary>
  private static IReadOnlyList<UPort> GoldPorts => GoldPortsLazy.Value;

  /// <summary>Gold-declared ports ∪ live engine projections, deduplicated by canonical form.</summary>
  private static IReadOnlyList<UPort> Universe(PortSide side)
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var result = new List<UPort>();
    foreach (var p in GoldPorts.Where(p => p.Structure.Side == side))
      if (seen.Add(p.Structure.Canonical()))
        result.Add(p);
    foreach (var p in FlowProbes.Live.Where(p => p.Structure.Side == side))
      if (seen.Add(p.Structure.Canonical()))
        result.Add(new UPort(p.Structure, "live projection"));
    return result;
  }

  private static readonly Lazy<IReadOnlyList<GEdge>> GoldEdgesLazy = new(() =>
  {
    var edges = new List<GEdge>();
    foreach (var gn in GoldList)
    {
      var g = gn.AsObject();
      var gid = g["id"]!.GetValue<string>();

      // Port index by both "Card.Id" and bare "Id" — the identity convention edges[].from/to use.
      var index = new Dictionary<string, (string Card, JsonObject Port)>(StringComparer.Ordinal);
      foreach (var cardKv in g["ports"]!.AsObject())
      {
        if (cardKv.Value is not JsonArray plist)
          continue;
        foreach (var pn in plist)
        {
          if (pn is not JsonObject p)
            continue;
          var pid = p["id"]?.GetValue<string>();
          if (pid is null)
            continue;
          index[$"{cardKv.Key}.{pid}"] = (cardKv.Key, p);
          index.TryAdd(pid, (cardKv.Key, p));
        }
      }

      if (g["edges"] is not JsonArray es)
        continue;
      foreach (var en in es)
      {
        if (en is not JsonObject e)
          continue;
        var fromRef = e["from"]!.GetValue<string>();
        var toRef = e["to"]!.GetValue<string>();
        if (!index.TryGetValue(fromRef, out var from) || !index.TryGetValue(toRef, out var to))
          continue;
        var (fromCard, fp) = from;
        var (toCard, tp) = to;
        var fromSide = SideOf(fp["side"]?.GetValue<string>());
        var toSide = SideOf(tp["side"]?.GetValue<string>());
        var fromStem = fp["stem"]?.GetValue<string>();
        var toStem = tp["stem"]?.GetValue<string>();
        if (fromSide is null || toSide is null || fromStem is null || toStem is null)
          continue;
        edges.Add(
          new GEdge(
            gid,
            e["id"]!.GetValue<string>(),
            e["mechanism"]!.GetValue<string>(),
            e["rule"]?.GetValue<string>(),
            e["tier"]!.GetValue<string>(),
            PortStructure.Of(fromSide.Value, fromStem, GoldAttrs(fp["attrs"]).ToArray()),
            PortStructure.Of(toSide.Value, toStem, GoldAttrs(tp["attrs"]).ToArray()),
            fromRef,
            toRef,
            fromCard,
            toCard
          )
        );
      }
    }
    return edges;
  });

  /// <summary>Every gold-declared edge whose two endpoints resolve to declared, side-bearing ports.</summary>
  private static IReadOnlyList<GEdge> GoldEdges => GoldEdgesLazy.Value;

  /// <summary>Report the population a sweep examined, and fail loudly if it is empty (the vacuity guard).</summary>
  private static void Examined(string prediction, string population, int count)
  {
    TestContext.Out.WriteLine($"{prediction}: examined {count} {population}.");
    Assert.That(
      count,
      Is.GreaterThan(0),
      $"{prediction} VACUOUS — the candidate population ({population}) is empty, so this sweep asserts "
        + "nothing. A prediction that passes because it had nothing to quantify over is worthless "
        + "(ADR-0004 §7): fix the population derivation, never accept the green."
    );
  }

  // ── P1 ──────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// <b>P1</b>, verbatim: <i>"every <c>emit:removal:*[manner=sacrificed]</c> (sac outlet) reaches all three
  /// consumer rungs — 'when sacrificed', 'when dies' (<c>removal:*[to=graveyard]</c>), 'when leaves
  /// battlefield' (<c>removal:*</c>) — by subsumption."</i> (ADR-0003 §5, O1: sacrifice ⊂ dies ⊂ LTB.)
  ///
  /// <para><b>Encoded predicate.</b> For every sac outlet in the universe (an emit whose stem is under the
  /// <c>removal</c> supergroup carrying <c>manner=sacrificed</c>), <see cref="PortFlowMatcher.SelectArm"/>
  /// selects an arm against all three consumer rungs built on that outlet's OWN stem:
  /// <c>consume:S[manner=sacrificed]</c>, <c>consume:S[to=graveyard]</c>, and bare <c>consume:S</c>.</para>
  ///
  /// <para><b>This sweep went RED on first execution, and the red was a REAL TOPOLOGY GAP</b>
  /// (interaction-judge, 2026-07-20). Chatterfang × Pitiless Plunderer's Treasure sac
  /// (<c>emit:removal:artifact[manner=sacrificed,to=graveyard]</c>) reached none of its three rungs, because
  /// <c>SacrificeDeathToTrigger</c> was written <c>E("removal:creature") &amp;&amp; C("removal:creature")</c>.
  /// The rungs are synthesized on the outlet's OWN stem, so this was never about corpus coverage — the
  /// matcher refused a <em>same-stem</em> removal pair the CR relates: sacrifice moves a <em>permanent</em>
  /// to its graveyard (CR 701.21a), "dies" is put-into-graveyard-from-battlefield for any permanent
  /// (CR 700.4), leaves-the-battlefield triggers are permanent-general (CR 603.6d), and CR 603.10b names
  /// "abilities that trigger when a player sacrifices a permanent" outright. The consumer rung is printed in
  /// bulk (45 oracle cards match "artifact you control is put into a graveyard from the battlefield"). The
  /// fix is in <see cref="PortFlowMatcher.SelectArm"/> — same stem, under the <c>removal</c> supergroup —
  /// and it materializes zero new edges in the current corpus. <b>P1's quantifier was NOT narrowed;</b>
  /// narrowing it to <c>removal:creature</c> would have hidden the bug behind a green.</para>
  ///
  /// <para><b>What would falsify it:</b> any sac outlet under <c>removal</c> that fails to reach any one of
  /// its three rungs — the CR ladder (sacrifice ⊂ dies ⊂ leaves-the-battlefield, ADR-0003 O1) broken for
  /// some object type. Cross-STEM subsumption (<c>removal:permanent</c> destroy → <c>removal:creature</c>
  /// dies, AMBER by CR 110.4) is deliberately NOT asserted here: it needs a type relation inside
  /// <c>SacrificeDeathFeedsTrigger</c> first, which the judge ruled a separate change.</para>
  /// </summary>
  [Test]
  public void P1_sac_outlet_reaches_all_three_consumer_rungs()
  {
    var outlets = Universe(PortSide.Emit)
      .Where(p => IsUnder(p.Structure.Stem, "removal") && p.Structure.Attr("manner") == "sacrificed")
      .ToList();

    Examined("P1", "emit:removal:*[manner=sacrificed] sac outlets", outlets.Count);

    // The quantifier is `removal:*`, so the universe must contain more than one removal stem — otherwise
    // this sweep silently degrades into the creature-only claim whose hardcode it exists to have caught.
    var stemsCovered = outlets.Select(p => p.Structure.Stem).Distinct(StringComparer.Ordinal).ToList();
    Assert.That(
      stemsCovered.Count,
      Is.GreaterThan(1),
      "P1 DEGENERATE — every sac outlet in the universe is on the same removal stem "
        + $"({string.Join(", ", stemsCovered)}), so the `removal:*` quantifier is testing exactly one stem. "
        + "That is the shape in which the removal:creature hardcode hid; re-derive before trusting a green."
    );

    var brokenRungs = new List<string>();
    var checkedRungs = 0;
    foreach (var outlet in outlets)
      foreach (var rung in Rungs(outlet.Structure.Stem))
      {
        checkedRungs++;
        if (PortFlowMatcher.SelectArm(outlet.Structure, rung.Structure) is null)
          brokenRungs.Add($"{outlet} ↛ {rung.Name} ({rung.Structure.Canonical()})");
      }

    Examined("P1", "(sac outlet × consumer rung) pairs", checkedRungs);

    Assert.That(
      brokenRungs,
      Is.Empty,
      "P1 FALSIFIED — a sacrifice emit failed to reach a rung it subsumes (ADR-0003 O1: sacrifice ⊂ dies ⊂ "
        + "leaves-the-battlefield; CR 701.21a / 700.4 / 603.6d / 603.10b, all permanent-general). This is a "
        + "hard failure resolved by the interaction-judge; never narrow the quantifier or the rung set to "
        + "make it pass — that is exactly how the removal:creature hardcode survived to 2026-07-20:\n  "
        + string.Join("\n  ", brokenRungs)
    );
  }

  /// <summary>The three consumer rungs P1 names, built on a given removal stem (ADR-0003 O1's ladder).</summary>
  private static IEnumerable<(string Name, PortStructure Structure)> Rungs(string stem)
  {
    yield return ("when sacrificed", PortStructure.Of(PortSide.Consume, stem, ("manner", "sacrificed")));
    yield return ("when dies", PortStructure.Of(PortSide.Consume, stem, ("to", "graveyard")));
    yield return ("when leaves battlefield", PortStructure.Of(PortSide.Consume, stem));
  }

  // ── P2 ──────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// <b>P2</b>, verbatim: <i>"every <c>emit:deployment:creature[token]</c> (token maker) reaches every
  /// <c>consume:objects:creature</c> (sac/fodder consumer) via the object store."</i>
  ///
  /// <para><b>Encoded predicate.</b> For every token maker in the universe (emit
  /// <c>deployment:creature[token=true]</c>) and every fodder consumer (a consume whose stem is a member of
  /// the <c>objects</c> supergroup — read from the scaffold, not hardcoded — restricted to
  /// <c>creature</c>), <see cref="PortFlowMatcher.SelectArm"/> selects the <c>TokenToSac</c> arm.</para>
  ///
  /// <para><b>RULED (interaction-judge, 2026-07-20): FALSIFIED AS STATED — "every consume:objects:creature"
  /// is too broad; the true claim is every consume that DECLARES the sac manner.</b> The <c>TokenToSac</c>
  /// arm asserts a <em>sacrifice</em> relation, and CR 701.21a defines that narrowly (a player sacrifices a
  /// permanent they control). A bare <c>consume:creature[control=you,qty=1]</c> is structurally
  /// indistinguishable from "a creature you control" as a subscription subject or a non-sac cost, so arming
  /// it would assert a sacrifice hop the structure does not support — a false-GREEN generator. The
  /// <c>manner</c> facet is the only structural carrier of the sac ROLE, and is therefore the right place to
  /// draw the line. (P2's underlying rules claim is sound: a token creature IS a permanent — CR 111.1 — and
  /// can be sacrificed to an arbitrary "sacrifice a creature" cost.)</para>
  ///
  /// <para><b>The falsifier is itself a DEFECT TO BE REPAIRED, not an accepted vocabulary split.</b> Nine
  /// hand-authored fodder consumes across the golds are, without exception, printed sacrifice costs
  /// ("Sacrifice a creature: …") whose <c>manner=sacrificed</c> the engine's own projection of the same
  /// cards recovers — the golds drop structure that IS in the oracle text, which is information loss, not a
  /// naming difference like <c>damage-dealt</c>/<c>damage</c>. Repairing them mutates
  /// <c>rollup/port-topology.json</c> (the <c>creature</c> stem gains a <c>manner</c> attr), which is owned
  /// elsewhere, so it is reported rather than fixed here: <c>chatterfang C4</c>, <c>ashnods AN1</c> (×2
  /// golds), <c>phyrexian PA1</c>, <c>viscera V1</c> (×2), <c>nantuko N1</c> (×2), <c>ruthless R2</c>.</para>
  ///
  /// <para><b>What would falsify what is now asserted:</b> (a) a token maker that fails to reach a
  /// sac-declaring fodder consume — the object store hop is broken; or (b) the gold-vocabulary defect being
  /// REPAIRED, which retires the pinned finding and makes P2's original universal quantifier re-derivable.</para>
  /// </summary>
  [Test]
  public void P2_token_maker_reaches_every_object_store_fodder_consumer()
  {
    var objectStems = SupergroupMembers("objects");
    Assert.That(
      objectStems,
      Does.Contain("creature"),
      "P2 cannot be posed — the scaffold's `objects` supergroup no longer lists the `creature` stem, so "
        + "'consume:objects:creature' does not resolve"
    );

    var makers = Universe(PortSide.Emit)
      .Where(p => p.Structure.Stem == "deployment:creature" && p.Structure.Attr("token") == "true")
      .ToList();
    var fodder = Universe(PortSide.Consume).Where(p => p.Structure.Stem == "creature").ToList();

    Examined("P2", "emit:deployment:creature[token] token makers", makers.Count);
    Examined("P2", "consume:objects:creature candidates", fodder.Count);

    var sacDeclaring = fodder.Where(p => p.Structure.Attr("manner") == "sacrificed").ToList();
    var bare = fodder.Where(p => p.Structure.Attr("manner") is null).ToList();

    Assert.That(
      sacDeclaring,
      Is.Not.Empty,
      "P2 VACUOUS — no consume:creature[manner=sacrificed] anywhere in the universe. The ruled-true claim "
        + "would quantify over nothing; the engine's fodder projection has stopped carrying the manner facet."
    );
    Assert.That(
      bare,
      Is.Not.Empty,
      "P2's pinned counterexample is GONE — every consume:objects:creature now declares a manner, i.e. the "
        + "gold-vocabulary DEFECT the 2026-07-20 ruling identified has been repaired. Re-derive P2's "
        + "original 'every consume:objects:creature' quantifier with the judge instead of leaving it retired."
    );

    var unreached = new List<string>();
    var checkedPairs = 0;
    foreach (var m in makers)
      foreach (var f in sacDeclaring)
      {
        checkedPairs++;
        if (PortFlowMatcher.SelectArm(m.Structure, f.Structure) != PortFlowMatcher.FlowArm.TokenToSac)
          unreached.Add($"{m} ↛ {f}");
      }

    Examined("P2", "(token maker × sac-declaring fodder consume) pairs", checkedPairs);

    // The pinned finding: a bare object-store consume reaches nothing, deliberately — arming it would
    // manufacture false GREENs against every "a creature you control" subscription.
    var wronglyArmed = new List<string>();
    foreach (var m in makers)
      foreach (var b in bare)
        if (PortFlowMatcher.SelectArm(m.Structure, b.Structure) is { } arm)
          wronglyArmed.Add($"{m} → {b} selects {arm}");

    Assert.Multiple(() =>
    {
      Assert.That(
        unreached,
        Is.Empty,
        "P2 FALSIFIED — a token maker does not reach a fodder consume that explicitly declares the sac "
          + "manner. The object-store hop (deployment:creature[token] → creature[manner=sacrificed], "
          + "TokenToSac) is broken. Judge-resolved; never weaken the arm:\n  "
          + string.Join("\n  ", unreached)
      );
      Assert.That(
        wronglyArmed,
        Is.Empty,
        "P2's pinned counterexample CHANGED — a token maker now reaches a bare consume:objects:creature "
          + "that declares no manner. Either the gold vocabulary gained the facet (good: re-derive P2's "
          + "original quantifier) or the TokenToSac guard was loosened (bad: that arms every 'a creature "
          + "you control' subscription as sac fodder). Judge-gated either way:\n  "
          + string.Join("\n  ", wronglyArmed)
      );
    });
  }

  // ── P3 ──────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// <b>P3</b>, verbatim: <i>"color is topology-invariant — it never appears on a stem, only as an
  /// attribute; a mono-U and a B/G combo have the same supergroup shape."</i>
  ///
  /// <para><b>Encoded predicate</b>, in three parts, all derived (no colour list is typed here — the values
  /// come from the rollup's own <c>color</c> axis <c>values_seen</c>):
  /// <list type="number">
  ///   <item><b>Never on a stem.</b> No stem segment, anywhere in the witnessed topology or the universe,
  ///     equals a witnessed colour value.</item>
  ///   <item><b>Always an attribute.</b> <c>color</c> is a witnessed attribute axis naming ≥1 stem.</item>
  ///   <item><b>Topology-invariant.</b> Re-colouring is a no-op on flow structure: for every ordered
  ///     (emit, consume) pair in the universe where either side carries <c>color</c>, substituting ANY other
  ///     witnessed colour value — or dropping the attribute entirely — leaves
  ///     <see cref="PortFlowMatcher.SelectArm"/>'s answer unchanged. That is the executable content of "a
  ///     mono-U and a B/G combo have the same supergroup shape": the shape is what the arms draw, and the
  ///     arms cannot see colour.</item>
  /// </list></para>
  ///
  /// <para><b>What would falsify it:</b> a stem named after a colour (<c>mana:blue</c>, a
  /// <c>deployment:creature:white</c> spine); the <c>color</c> axis vanishing from the witnessed axes; or
  /// ANY arm-selection branch keying on a colour value, which would make two decks of different colours
  /// produce structurally different graphs. (Colour compatibility legitimately lives in the
  /// <c>ManaToPay</c> GUARD — ADR-0003 §5's structure/guard split — which this sweep deliberately does not
  /// touch: the guard decides whether a hop HOLDS, the arm decides what shape the graph HAS.)</para>
  /// </summary>
  [Test]
  public void P3_color_is_topology_invariant()
  {
    Assert.That(
      Topology.AttributeAxes.TryGetValue("color", out var colorAxis),
      Is.True,
      "P3 cannot be posed — the witnessed topology has no `color` attribute axis at all, so 'color is only "
        + "an attribute' has no referent"
    );
    var colors = colorAxis!.ValuesSeen.ToList();
    Examined("P3", "witnessed color values", colors.Count);

    Assert.That(
      colorAxis.Stems,
      Is.Not.Empty,
      "P3 part 2 FALSIFIED — the `color` axis names no stems; colour is no longer carried as an attribute "
        + "anywhere in the witnessed graph"
    );

    // Part 1 — never on a stem.
    var allStems = Topology
      .Stems.Keys.Concat(Universe(PortSide.Emit).Select(p => p.Structure.Stem))
      .Concat(Universe(PortSide.Consume).Select(p => p.Structure.Stem))
      .Distinct(StringComparer.Ordinal)
      .OrderBy(s => s, StringComparer.Ordinal)
      .ToList();
    Examined("P3", "distinct stems checked for a colour segment", allStems.Count);

    var colored = allStems
      .Where(s => s.Split(':').Any(seg => colors.Contains(seg, StringComparer.OrdinalIgnoreCase)))
      .ToList();

    // Part 3 — re-colouring cannot change the arm.
    var emits = Universe(PortSide.Emit);
    var consumes = Universe(PortSide.Consume);
    var variances = new List<string>();
    var checkedSubstitutions = 0;
    foreach (var e in emits)
      foreach (var c in consumes)
      {
        if (e.Structure.Attr("color") is null && c.Structure.Attr("color") is null)
          continue;
        var baseline = PortFlowMatcher.SelectArm(e.Structure, c.Structure);
        foreach (var v in colors.Append(null))
        {
          var e2 = Recolor(e.Structure, v);
          var c2 = Recolor(c.Structure, v);
          checkedSubstitutions++;
          var arm = PortFlowMatcher.SelectArm(e2, c2);
          if (arm != baseline)
            variances.Add(
              $"{e.Structure.Canonical()} → {c.Structure.Canonical()} selects {Describe(baseline)}, but "
                + $"re-coloured to {e2.Canonical()} → {c2.Canonical()} selects {Describe(arm)}"
            );
        }
      }

    Examined("P3", "re-colouring substitutions over colour-carrying pairs", checkedSubstitutions);

    Assert.Multiple(() =>
    {
      Assert.That(
        colored,
        Is.Empty,
        "P3 part 1 FALSIFIED — colour appears in an is-a STEM, not only as an attribute. ADR-0003 §2/§14: "
          + "the colon spine is is-a taxonomy; colour is an orthogonal facet. Offending stem(s): "
          + string.Join(", ", colored)
      );
      Assert.That(
        variances,
        Is.Empty,
        "P3 part 3 FALSIFIED — flow-arm selection is NOT colour-blind, so a mono-U and a B/G combo do not "
          + "have the same shape. Colour compatibility belongs in an arm's GUARD, never in SelectArm's "
          + "structural half (ADR-0003 §5):\n  "
          + string.Join("\n  ", variances)
      );
    });
  }

  private static string Describe(PortFlowMatcher.FlowArm? arm) => arm?.ToString() ?? "no arm";

  /// <summary>The structure with its <c>color</c> attribute set to <paramref name="value"/>, or dropped
  /// when null. Structures that never carried colour are returned untouched.</summary>
  private static PortStructure Recolor(PortStructure s, string? value)
  {
    if (s.Attr("color") is null)
      return s;
    var attrs = s.Attributes.Where(a => a.Key != "color").Select(a => (a.Key, a.Value)).ToList();
    if (value is not null)
      attrs.Add(("color", value));
    return PortStructure.Of(s.Side, s.Stem, attrs.ToArray());
  }

  // ── P4 ──────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// <b>P4</b>, verbatim: <i>"irreducible cross-resource bridges (<c>structure:untap → mana</c>) stay AMBER
  /// by ceiling."</i> (ADR-0003 §6.4: "edges no lattice relates (untap→mana, AMBER ceiling, CR 107.4) —
  /// which remain curated, tier-ceilinged, and cited".)
  ///
  /// <para><b>Encoded predicate.</b> Population = every gold-declared edge whose <c>mechanism</c> is
  /// <c>bridge</c> AND which the structural lattice cannot relate (<see cref="PortFlowMatcher.SelectArm"/>
  /// returns no arm for the pair). "Irreducible" IS that second condition — §6.4's own definition — so it is
  /// computed, not asserted. Every such edge must be tiered <c>AMBER</c>. The named instance
  /// (<c>structure:untap → mana</c>) is additionally required to be present and AMBER, so P4's own example
  /// cannot silently leave the corpus.</para>
  ///
  /// <para><b>Why REDUCIBLE bridges are excluded, and how that is kept honest.</b> A bridge the lattice DOES
  /// relate is by construction not one of P4's "irreducible" bridges — <c>bridge:sacrifice-to-dies</c> is
  /// the live case: the <c>SacrificeDeathToTrigger</c> arm now subsumes it structurally, which is exactly
  /// what retired the curated consume→consume sac→dies label bridge. Those edges are not dropped in
  /// silence: they are counted, reported, and each one is asserted to genuinely select an arm — so
  /// "excluded as reducible" is a fact the sweep proves, not a category it grants itself.</para>
  ///
  /// <para><b>What would falsify it:</b> a bridge edge that no lattice relation covers, tiered GREEN — i.e.
  /// a curated cross-resource hop claiming full certainty without a subsumption to rest on. That is the
  /// false-GREEN class the AMBER ceiling exists to prevent (CR 107.4: untapping is not mana).</para>
  /// </summary>
  [Test]
  public void P4_irreducible_cross_resource_bridges_stay_amber()
  {
    var bridges = GoldEdges.Where(e => e.Mechanism == "bridge").ToList();
    Examined("P4", "gold-declared bridge edges", bridges.Count);

    var irreducible = bridges.Where(e => LatticeRelates(e) is null).ToList();
    var reducible = bridges.Where(e => LatticeRelates(e) is not null).ToList();

    Examined("P4", "IRREDUCIBLE bridge edges (no flow arm relates the pair)", irreducible.Count);
    TestContext.Out.WriteLine(
      $"P4: {reducible.Count} bridge edge(s) excluded as REDUCIBLE (an arm relates them): "
        + string.Join("; ", reducible.Select(e => $"{e} [{LatticeRelates(e)}]"))
    );

    var untapToMana = irreducible
      .Where(e => e.From.Stem == "structure:untap" && e.To.Stem == "mana")
      .ToList();
    Examined("P4", "structure:untap → mana bridges (P4's named instance)", untapToMana.Count);

    var greens = irreducible.Where(e => e.Tier != "AMBER").ToList();

    Assert.Multiple(() =>
    {
      Assert.That(
        greens,
        Is.Empty,
        "P4 FALSIFIED — an irreducible cross-resource bridge broke its AMBER ceiling. No lattice relation "
          + "covers these pairs, so nothing proves the hop; a GREEN here is a false GREEN by construction "
          + "(ADR-0003 §6.4, CR 107.4). Judge-resolved — never relax the ceiling to match the fixture:\n  "
          + string.Join("\n  ", greens.Select(e => e.ToString()))
      );
      Assert.That(
        untapToMana.All(e => e.Tier == "AMBER"),
        Is.True,
        "P4 FALSIFIED on its own named instance — a structure:untap → mana bridge is not AMBER:\n  "
          + string.Join("\n  ", untapToMana.Where(e => e.Tier != "AMBER").Select(e => e.ToString()))
      );
      // The exclusion is proven, not granted.
      Assert.That(
        reducible.Where(e => LatticeRelates(e) is null).ToList(),
        Is.Empty,
        "P4 bookkeeping bug — an edge was excluded as reducible without an arm relating it"
      );
      // …and an excluded bridge must also not be CROSS-resource, so a future loosened arm cannot quietly
      // move a genuinely cross-supergroup bridge out of the AMBER-ceiling population (judge, 2026-07-20).
      Assert.That(
        reducible
          .Where(e => SupergroupOf(e.From.Stem) is null || SupergroupOf(e.From.Stem) != SupergroupOf(e.To.Stem))
          .Select(e => $"{e} [{SupergroupOf(e.From.Stem) ?? "?"} → {SupergroupOf(e.To.Stem) ?? "?"}]")
          .ToList(),
        Is.Empty,
        "P4 SCOPE BREACH — a bridge excluded from the AMBER-ceiling population as 'reducible' crosses "
          + "supergroups (or has no resolvable supergroup). An arm relating a genuinely CROSS-resource pair "
          + "is precisely what P4 forbids being trusted; do not let the exclusion absorb it"
      );
    });
  }

  /// <summary>The flow arm relating an edge's endpoints, or null when the lattice does not relate them
  /// (an emit→emit or consume→consume bridge is never lattice-related — the matcher is emit × consume).</summary>
  private static PortFlowMatcher.FlowArm? LatticeRelates(GEdge e) =>
    e.From.Side == PortSide.Emit && e.To.Side == PortSide.Consume
      ? PortFlowMatcher.SelectArm(e.From, e.To)
      : null;

  // ── P5 ──────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// <b>P5</b>, verbatim: <i>"a self-watching subscription (<c>subject=self</c>) fed by a broad emit ('a
  /// creature') tiers AMBER (Overlaps), never GREEN."</i> (ADR-0003 §6.3, identity guards; CR 400.7 —
  /// object identity is not preserved across a zone change, so the operator cannot pin "a creature" to
  /// "this creature".)
  ///
  /// <para><b>Encoded predicate.</b> Population = every gold-declared edge whose CONSUME endpoint carries
  /// <c>subject=self</c> — a self-watching subscription. Each such edge is AMBER <b>unless</b> its emit is a
  /// card-defined witness, which is precisely what "fed by a BROAD emit" excludes: ADR-0004's
  /// <c>PortEdge.CardDefined</c> discharges the same-card witness OR the created-object witness
  /// (<c>to.Grafter == from.Card</c>, CR 707.2/603.6a — the consuming object <em>exists only as the product
  /// of that emit</em>). An emit that creates the very object doing the watching is the opposite of broad:
  /// the subject is pinned by construction. Everything else — a blink of "a creature you control", a copy
  /// feeding a foreign cast trigger, an extra-combat phase feeding "whenever this creature attacks" — is
  /// broad, and must tier AMBER.</para>
  ///
  /// <para>Both branches are asserted non-empty, so the sweep cannot pass by having classified every edge
  /// into the exempt bucket.</para>
  ///
  /// <para><b>What would falsify it:</b> a GREEN edge into a <c>subject=self</c> consume whose emit is NOT
  /// that object's own creator — the operator claiming it can prove "a creature" is "this creature". That is
  /// the guard:self-watch false-GREEN class (Deadeye × Peregrine's E1 is the standing acceptance case).</para>
  /// </summary>
  [Test]
  public void P5_self_watching_subscription_fed_by_a_broad_emit_stays_amber()
  {
    var selfWatched = GoldEdges
      .Where(e => e.To.Side == PortSide.Consume && e.To.Attr("subject") == "self")
      .ToList();
    Examined("P5", "gold-declared edges into a subject=self subscription", selfWatched.Count);

    // "fed by a broad emit" — everything except the card's own causality. The exemption is keyed on the
    // WITNESS ADR-0004 actually discharges, not merely on the `card-defined` label: same-card, or the emit
    // creates the very object doing the watching (a copy/token graft — CR 707.2/603.6b). The mechanism
    // string must ALSO agree, so a gold cannot ride out by mislabelling an ordinary edge (judge, 2026-07-20).
    var ownCausality = selfWatched.Where(OwnCausalityWitness).ToList();
    var broadFed = selfWatched.Where(e => !OwnCausalityWitness(e)).ToList();

    Examined("P5", "BROAD-fed self-watching edges", broadFed.Count);
    Assert.That(
      ownCausality,
      Is.Not.Empty,
      "P5's exempt branch is empty — no self-watching edge is fed by its own card's causality. That is not "
        + "a failure of the prediction, but it means the broad/own-causality split is no longer exercised; "
        + "confirm the split still discriminates before trusting this sweep."
    );
    TestContext.Out.WriteLine(
      $"P5: {ownCausality.Count} edge(s) exempt as own-causality (same-card / created-object witness): "
        + string.Join("; ", ownCausality.Select(e => e.ToString()))
    );

    var falseGreens = broadFed.Where(e => e.Tier == "GREEN").ToList();

    Assert.That(
      falseGreens,
      Is.Empty,
      "P5 FALSIFIED — a self-watching (subject=self) subscription fed by a BROAD emit tiers GREEN. The "
        + "operator cannot prove 'a creature' is 'this creature' (CR 400.7); the honest verdict is Overlaps "
        + "→ AMBER. This is the guard:self-watch false-GREEN class. Judge-resolved — never downgrade the "
        + "predicate to accommodate it:\n  "
        + string.Join("\n  ", falseGreens.Select(e => e.ToString()))
    );
  }

  /// <summary>
  /// Does the emit witness the watched object's identity by the card's own causality — ADR-0004's
  /// <c>PortEdge.CardDefined</c> obligation? Either the <b>same-card witness</b> (both endpoints on one
  /// card, CR 601.2h/608.2c) or the <b>created-object witness</b> (the consuming object exists only as this
  /// emit's product: a copy graft or a token creation, CR 707.2 / 603.6b). The declared
  /// <c>mechanism == "card-defined"</c> must corroborate — a structural witness with a contradicting
  /// mechanism, or a `card-defined` label with no witness, is NOT exempt and must tier AMBER.
  /// </summary>
  private static bool OwnCausalityWitness(GEdge e)
  {
    var sameCard = string.Equals(e.FromCard, e.ToCard, StringComparison.Ordinal);
    var createsTheWatchedObject =
      e.From.Stem == "copy"
      || (IsUnder(e.From.Stem, "deployment") && e.From.Attr("token") == "true");
    return e.Mechanism == "card-defined" && (sameCard || createsTheWatchedObject);
  }

  // ── P6 ──────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// <b>P6</b>, verbatim: <i>"every one of the six holes has ≥1 witnessable card in the corpus (else the
  /// hole is a taxonomy error, not a coverage gap)."</i>
  ///
  /// <para><b>Disposition: RE-EXPRESSED (issue #27 option 1), not retired.</b> ADR-0004 §7 / issue #26
  /// deleted the <c>holes</c> registry — all six had reached <c>status: witnessed</c>, which is exactly what
  /// P6 predicted — so P6 cannot be encoded against the registry it names. The alternative sink, the derived
  /// backlog (<c>projected − served − asserted-unarmable</c>, issue #32), is not built. What DOES exist is
  /// the six holes' <c>proposed_stem</c>s, which are now ordinary witnessed stems in the rollup, so P6 is
  /// re-expressed against those: <b>each of the six carries ≥1 witnessing gold, and that gold names a real
  /// card and actually declares a port on that stem.</b> That is P6's content — a hole with a card behind it
  /// is a coverage gap that closed, a hole with none was a taxonomy error — and it keeps working: if a stem
  /// loses its last witness the sweep goes red, where retirement would have gone silent.</para>
  ///
  /// <para>The six stems are a frozen historical list, NOT a revived registry: no status, no priority, no
  /// backlog semantics, nothing to hand-maintain. They are the six <c>proposed_stem</c> values recorded in
  /// <c>git show 12186ed1^:tests/magic-ast-tests/Fixtures/Interactions/topology-scaffold.json</c> —
  /// cost-modification → <c>modification:spell</c>, restriction-grant → <c>modification:restriction</c>,
  /// prevention → <c>modification:prevention</c>, library-search → <c>cards:search</c>, library-selection →
  /// <c>cards:select</c>, non-play-zone-move → <c>recur</c>. Adding a seventh entry here would be reviving
  /// the registry; don't.</para>
  ///
  /// <para><b>What would falsify it:</b> one of the six stems disappearing from the witnessed topology,
  /// losing its witnesses, or being witnessed only by a gold that declares no port on it and names no card —
  /// i.e. a "closed" hole that turns out to rest on nothing, which is P6's own definition of a taxonomy
  /// error.</para>
  /// </summary>
  [Test]
  public void P6_every_former_capability_hole_rests_on_a_real_card_witness()
  {
    string[] holeStems =
    [
      "modification:spell",
      "modification:restriction",
      "modification:prevention",
      "cards:search",
      "cards:select",
      "recur",
    ];

    Examined("P6", "former capability-hole stems", holeStems.Length);

    var goldsById = GoldList.ToDictionary(g => g["id"]!.GetValue<string>(), g => g.AsObject());
    var failures = new List<string>();
    var witnessesChecked = 0;

    foreach (var stem in holeStems)
    {
      if (!Topology.Stems.TryGetValue(stem, out var entry))
      {
        failures.Add($"{stem}: absent from the witnessed topology entirely");
        continue;
      }
      if (entry.Status != "witnessed")
      {
        failures.Add($"{stem}: status is '{entry.Status}', not 'witnessed'");
        continue;
      }
      if (entry.Witnesses is not { Count: > 0 })
      {
        failures.Add($"{stem}: witnessed status with no witnessing gold");
        continue;
      }

      // "≥1 witnessable card": a witnessing gold that names a card AND declares a port on this stem.
      var grounded = entry
        .Witnesses.Where(w =>
          goldsById.TryGetValue(w, out var g)
          && g["cards"] is JsonArray cards
          && cards.Count > 0
          && g["ports"]!
            .AsObject()
            .Any(cardKv =>
              cardKv.Value is JsonArray ps
              && ps.Any(p => p?["stem"]?.GetValue<string>() == stem)
            )
        )
        .ToList();
      witnessesChecked += entry.Witnesses.Count;

      if (grounded.Count == 0)
        failures.Add(
          $"{stem}: witnessed by [{string.Join(", ", entry.Witnesses)}] but no witnessing gold both names "
            + "a card and declares a port on the stem"
        );
    }

    Examined("P6", "witnessing gold references resolved", witnessesChecked);

    Assert.That(
      failures,
      Is.Empty,
      "P6 FALSIFIED — a former capability hole no longer rests on a real card witness. Per P6's own terms "
        + "that makes it a TAXONOMY ERROR rather than a closed coverage gap, and the stem's place in the "
        + "ontology has to be re-derived (judge-gated):\n  "
        + string.Join("\n  ", failures)
    );
  }

  // ── the deletion, enforced ──────────────────────────────────────────────────────────────────────

  /// <summary>
  /// ADR-0004 §7 (issue #27): with P1–P6 executable, <c>connectivity_predictions</c> is DELETED from
  /// <c>topology-scaffold.json</c>. This is the sibling of
  /// <c>TopologyRollupContractTests.The_scaffold_carries_no_declared_half</c>: a prediction that lives in a
  /// data file is checked by nothing and is therefore not falsifiable — which is the entire finding §7
  /// recorded about it. Re-adding the section fails here, and the remedy is to write the sweep, not the
  /// prose.
  /// </summary>
  [Test]
  public void The_scaffold_no_longer_carries_connectivity_predictions()
  {
    Assert.That(
      Scaffold["connectivity_predictions"],
      Is.Null,
      "topology-scaffold.json re-declared `connectivity_predictions`, deleted by ADR-0004 §7 (issue #27). "
        + "P1–P6 are executable sweeps in this fixture; a seventh prediction belongs here as a [Test] that "
        + "names what would falsify it, not as a string in a file nothing executes."
    );
  }

  // ── shared helpers ──────────────────────────────────────────────────────────────────────────────

  /// <summary>Is <paramref name="stem"/> the supergroup itself or a descendant of it on the is-a spine?</summary>
  private static bool IsUnder(string stem, string supergroup) =>
    stem == supergroup || stem.StartsWith(supergroup + ":", StringComparison.Ordinal);

  /// <summary>
  /// The stems a supergroup claims that its NAME does not already imply, read from the scaffold's
  /// <c>supergroups[*].stems</c> — the one membership fact no gold can witness (ADR-0004 §7 keeps exactly
  /// this in the scaffold). Prefix-named members resolve by name via <see cref="IsUnder"/>.
  /// </summary>
  /// <summary>
  /// The supergroup a stem belongs to, resolved exactly the way ADR-0004 §7 leaves it resolvable: by NAME
  /// for the prefix-named spines (<c>removal:*</c>, <c>deployment:*</c>, <c>modification:*</c>,
  /// <c>structure:*</c>) and by the scaffold's explicit <c>stems</c> membership list otherwise. Null for the
  /// stems that legitimately have no supergroup (<c>cast</c>, <c>copy</c>, <c>recur</c>, and the
  /// <c>event_verbs_no_supergroup</c> trio).
  /// </summary>
  private static string? SupergroupOf(string stem)
  {
    if (Scaffold["supergroups"] is not JsonObject groups)
      return null;
    foreach (var kv in groups)
    {
      if (kv.Key.StartsWith('$'))
        continue;
      if (IsUnder(stem, kv.Key))
        return kv.Key;
      if (SupergroupMembers(kv.Key).Contains(stem, StringComparer.Ordinal))
        return kv.Key;
    }
    return null;
  }

  private static IReadOnlyList<string> SupergroupMembers(string supergroup) =>
    (Scaffold["supergroups"]?[supergroup]?["stems"] as JsonArray)
      ?.Select(n => n!.GetValue<string>())
      .ToList()
    ?? [];
}
