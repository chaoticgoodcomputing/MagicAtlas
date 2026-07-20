namespace MagicAST.Tests.Tests.InteractionRollup;

using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

/// <summary>
/// ADR-0004 §1 — the standing proof that an asserted-absence (<c>no_arm[P]</c>) claim is
/// <b>non-vacuous</b>. The claim's whole risk is passing for the wrong reason: an empty probe universe, a
/// port that never resolved, a probe set too coarse to reach the facet an arm keys on. Each of those is a
/// test here, so the assertion runner cannot degrade into a green that tests nothing.
///
/// <para>These are stateless invariants over the live matcher + the regenerated rollup — no counts to
/// ratchet, no baseline to shrink. The one list that IS enumerated (the arms) is enumerated from the
/// <see cref="PortFlowMatcher.FlowArm"/> enum itself, so a new arm is in scope the moment it is declared.</para>
/// </summary>
[TestFixture]
public class NoArmNonVacuityTests
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

  private static readonly PortTopology Topology = TopologyStep.Create()(
    (GoldList, LoadTopologyScaffoldStep.Create(ScaffoldPath)())
  ).Item2;

  [Test]
  public void The_probe_universe_is_non_empty_on_both_sides()
  {
    var witnessed = FlowProbes.WitnessedStems(Topology);
    var emits = FlowProbes.For(Topology, PortSide.Emit);
    var consumes = FlowProbes.For(Topology, PortSide.Consume);

    Assert.Multiple(() =>
    {
      Assert.That(witnessed, Is.Not.Empty, "the rollup contributed no witnessed stems — every absence claim would be vacuous");
      Assert.That(
        FlowProbes.Live,
        Is.Not.Empty,
        "the live projection contributed no structures — the sentinel corpus or PortWalk stopped producing structures"
      );
      Assert.That(emits, Is.Not.Empty);
      Assert.That(consumes, Is.Not.Empty);
    });
  }

  /// <summary>
  /// The probe set must carry FACETS, not just bare stems: several arms are guarded by an attribute
  /// (<c>creature[manner=sacrificed]</c>, <c>cast[role=trigger]</c>, <c>deployment:creature[event=etb]</c>).
  /// A bare-stem-only universe would silently miss an arm added behind such a guard.
  /// </summary>
  [Test]
  public void The_probe_universe_carries_attribute_facets_not_just_bare_stems()
  {
    var faceted = FlowProbes.For(Topology, PortSide.Consume).Where(p => p.Structure.Attributes.Count > 0).ToList();
    Assert.That(
      faceted,
      Is.Not.Empty,
      "no consume probe carries any attribute — an arm guarded by a facet would be unreachable by the probes"
    );
  }

  /// <summary>
  /// <b>The detector detects.</b> Every arm the matcher declares must be selectable by SOME probe pair in
  /// the universe. This is what makes an empty <c>ArmsFor</c> result meaningful: the universe demonstrably
  /// reaches every arm, so "no arm selected" is a statement about the asserted port, not about the probes.
  /// A newly added arm that no probe pair can reach fails HERE, loudly, instead of quietly weakening every
  /// absence gold.
  /// </summary>
  [Test]
  public void Every_declared_flow_arm_is_selectable_by_some_probe_pair()
  {
    var covered = FlowProbes.ArmCoverage(Topology);
    var missing = Enum.GetValues<PortFlowMatcher.FlowArm>().Where(a => !covered.ContainsKey(a)).ToList();

    Assert.That(
      missing,
      Is.Empty,
      "flow arm(s) unreachable by the no_arm probe universe — an absence claim could pass merely because "
        + "nothing in the universe can select them: "
        + string.Join(", ", missing)
    );
  }

  /// <summary>
  /// The positive control for <see cref="FlowProbes.ArmsFor"/>: a port that IS armed must come back with
  /// arms. <c>emit:mana</c> feeds a mana cost (<see cref="PortFlowMatcher.FlowArm.ManaToPay"/>), so a green
  /// here proves the evaluation path used by every <c>no_arm</c> claim reports hits when hits exist — the
  /// exact failure mode ("it passed because the checker never checks") this whole fixture exists to rule out.
  /// </summary>
  [Test]
  public void An_armed_port_is_reported_as_armed_by_the_same_evaluation_path()
  {
    var armed = PortStructure.Of(PortSide.Emit, "mana");
    var hits = FlowProbes.ArmsFor(armed, FlowProbes.For(Topology, PortSide.Consume));

    Assert.That(hits, Is.Not.Empty, "emit:mana selected no arm — the no_arm evaluation path is broken/vacuous");
    Assert.That(hits.Select(h => h.Arm), Does.Contain(PortFlowMatcher.FlowArm.ManaToPay));
  }

  /// <summary>
  /// The negative control, stated over the SAME evaluation path: the port asserted absent by the shipped
  /// gold (<c>rat-colony-deck-construction-terminal</c>) selects nothing. Duplicated deliberately from the
  /// gold's own assertion — this fixture is where the two controls sit side by side, so a change that makes
  /// both come back empty (the vacuity failure) is visible as the positive control going red.
  /// </summary>
  [Test]
  public void The_asserted_absent_port_selects_nothing_in_either_spelling()
  {
    var consumes = FlowProbes.For(Topology, PortSide.Consume);

    Assert.Multiple(() =>
    {
      // The gold's canonical ADR-0003 spelling…
      Assert.That(
        FlowProbes.ArmsFor(PortStructure.Of(PortSide.Emit, "deck-construction"), consumes),
        Is.Empty,
        "emit:deck-construction is armed — see rat-colony-deck-construction-terminal"
      );
      // …and the spelling the ENGINE actually emits, which is the one a future family would arm.
      Assert.That(
        FlowProbes.ArmsFor(PortStructure.Of(PortSide.Emit, "anynumberindeck"), consumes),
        Is.Empty,
        "emit:anynumberindeck is armed — see rat-colony-deck-construction-terminal"
      );
      // The coarse label must still be projected UNSTRUCTURED — if a family started recognizing it, the
      // absence judgment has to be re-derived rather than silently carried.
      Assert.That(
        FlowProbes.UnstructuredLabels,
        Does.Contain(FlowProbes.CoarseKey(PortSide.Emit, "emit:anynumberindeck")),
        "the anyNumberInDeck totality fallback is gone — a family now structures it; re-derive the gold"
      );
    });
  }
}
