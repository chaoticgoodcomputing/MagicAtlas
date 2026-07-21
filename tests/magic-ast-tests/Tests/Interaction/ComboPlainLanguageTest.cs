namespace MagicAST.Interaction.Tests;

using MagicAST.AST.References;
using MagicAST.Interaction;

/// <summary>
/// ADR 0004 §5.3 — <b>the axis vector IS the reason</b>. <see cref="ComboPlainLanguage"/> replaces the
/// hand-written <c>reason</c> prose that used to be pinned per combo, so it must be (a) total, (b) pure,
/// and (c) <b>in agreement with the engine's own <see cref="PortCycle.LimitingReason"/></b>.
///
/// <para>(c) is the load-bearing half and is written as a <b>metamorphic</b> check, not an oracle: we do
/// not assert what the right sentence is (that is copy, pending the owner's sign-off), we assert that the
/// renderer and the engine never disagree about WHICH axis is speaking. This is the same pattern the
/// retired <c>LimitingHopAgreesWithEngineTest</c> applied to the limiting hop, and it exists for the same
/// reason: a second implementation of a rule, kept honest only by a doc comment, is exactly the ADR-0004
/// failure mode.</para>
/// </summary>
[TestFixture]
public class ComboPlainLanguageTest
{
  /// <summary>The engine's <see cref="PortCycle.LimitingReason"/> vocabulary → the rendered sentence.
  /// Every §8 cycle-level reason must appear here; anything else is an EDGE-level reason, which the
  /// renderer deliberately does not speak for.</summary>
  private static readonly IReadOnlyDictionary<string, string> ReasonToSentence = new Dictionary<
    string,
    string
  >(StringComparer.Ordinal)
  {
    ["gated (rate-limit / intervening-if)"] = ComboPlainLanguage.Gated,
    ["tap (not renewed by an untapper)"] = ComboPlainLanguage.TapNotRenewed,
    ["mana-negative"] = ComboPlainLanguage.ManaNegative,
    ["life-negative"] = ComboPlainLanguage.LifeNegative,
    ["net-zero filter (no surplus)"] = ComboPlainLanguage.NetZero,
    ["unfed co-cost"] = ComboPlainLanguage.UnfedCoCost,
  };

  private static PortNode Port(string label, PortSide side, bool gated = false, bool tapGated = false) =>
    new()
    {
      Card = "A",
      Label = label,
      Side = side,
      Gated = gated,
      TapGated = tapGated,
      Identity = $"A::{label}::{gated}::{tapGated}",
    };

  private static PortEdge GreenEdge(bool gated = false, bool tapGated = false) =>
    new()
    {
      From = Port("emit:token:artifact:treasure", PortSide.Emit, gated, tapGated),
      To = Port("consume:whatever", PortSide.Consume),
      Provenance = EdgeProvenance.RulesDefined,
      Overlap = FilterRelation.Overlaps,
      Reliability = Trilean.Yes,
    };

  private static PortCycle Cycle(
    bool coCosts = true,
    bool balanced = true,
    bool lifeBalanced = true,
    bool productive = true,
    bool gated = false,
    bool tapGated = false,
    bool tapRenewed = false
  ) =>
    new()
    {
      Edges = [GreenEdge(gated, tapGated)],
      CoCostsSatisfied = coCosts,
      Balanced = balanced,
      LifeBalanced = lifeBalanced,
      Productive = productive,
      TapRenewed = tapRenewed,
    };

  // ── (a) totality + the sentences themselves ───────────────────────────────────────────────────────

  [Test]
  public void All_axes_holding_renders_as_certified_infinite()
  {
    var v = ComboAxisVector.FromCycle(Cycle());
    Assert.Multiple(() =>
    {
      Assert.That(v.AllHold, Is.True);
      Assert.That(v.FailingAxes, Is.Empty);
      Assert.That(ComboPlainLanguage.Describe(v), Is.EqualTo("certified infinite"));
      Assert.That(ComboPlainLanguage.DescribeAll(v), Is.Empty);
    });
  }

  [Test]
  public void Each_axis_renders_its_own_sentence()
  {
    Assert.Multiple(() =>
    {
      Assert.That(
        ComboPlainLanguage.Describe(ComboAxisVector.FromCycle(Cycle(balanced: false))),
        Is.EqualTo("loop closes, but it costs more mana than it makes")
      );
      Assert.That(
        ComboPlainLanguage.Describe(ComboAxisVector.FromCycle(Cycle(lifeBalanced: false))),
        Is.EqualTo("loop closes, but it drains more life than it gains")
      );
      Assert.That(
        ComboPlainLanguage.Describe(ComboAxisVector.FromCycle(Cycle(tapGated: true))),
        Is.EqualTo("needs a way to untap between iterations")
      );
      Assert.That(
        ComboPlainLanguage.Describe(ComboAxisVector.FromCycle(Cycle(coCosts: false))),
        Is.EqualTo("needs another card to pay one of its costs")
      );
      Assert.That(
        ComboPlainLanguage.Describe(ComboAxisVector.FromCycle(Cycle(productive: false))),
        Is.EqualTo("repeats, but produces nothing extra each time")
      );
      Assert.That(
        ComboPlainLanguage.Describe(ComboAxisVector.FromCycle(Cycle(gated: true))),
        Is.EqualTo("can only fire once per turn")
      );
    });
  }

  /// <summary>A renewed tap gate is discharged (ADR-0002 §8), so the cycle is firable and says nothing
  /// about untapping — the renderer must not speak from the raw <c>TapGated</c> flag.</summary>
  [Test]
  public void A_renewed_tap_gate_is_not_reported()
  {
    var v = ComboAxisVector.FromCycle(Cycle(tapGated: true, tapRenewed: true));
    Assert.That(ComboPlainLanguage.Describe(v), Is.EqualTo("certified infinite"));
  }

  /// <summary>A hard gate and a tap gate on the same cycle: the hard gate wins, because it is the one
  /// that can never be discharged. (This is the ordering <see cref="PortCycle.LimitingReason"/> uses.)</summary>
  [Test]
  public void A_hard_gate_outranks_an_unrenewed_tap_gate()
  {
    var v = ComboAxisVector.FromCycle(Cycle(gated: true, tapGated: true));
    Assert.That(ComboPlainLanguage.Describe(v), Is.EqualTo("can only fire once per turn"));
  }

  [Test]
  public void Failing_axes_are_reported_in_the_canonical_order()
  {
    var v = ComboAxisVector.FromCycle(Cycle(coCosts: false, balanced: false, gated: true));
    Assert.That(v.FailingAxes, Is.EqualTo(new[] { "Firable", "CoCostsSatisfied", "Balanced" }));
  }

  [Test]
  public void DescribeAll_reports_every_failing_axis()
  {
    var v = ComboAxisVector.FromCycle(Cycle(coCosts: false, balanced: false));
    Assert.That(
      ComboPlainLanguage.DescribeAll(v),
      Is.EqualTo(
        new[]
        {
          "needs another card to pay one of its costs",
          "loop closes, but it costs more mana than it makes",
        }
      )
    );
  }

  // ── (c) the metamorphic relation: the renderer and the engine never disagree ───────────────────────

  /// <summary>
  /// Over the FULL (§8-flag × gate-shape) space: whenever the engine names a cycle-level limiting reason,
  /// the renderer must render that same reason's sentence; whenever every axis holds, it must say
  /// certified infinite and the engine must name no cycle-level reason.
  /// </summary>
  [Test]
  [Combinatorial]
  public void The_renderer_and_the_engine_never_disagree_about_which_axis_speaks(
    [Values(true, false)] bool coCosts,
    [Values(true, false)] bool balanced,
    [Values(true, false)] bool lifeBalanced,
    [Values(true, false)] bool productive,
    [Values(true, false)] bool gated,
    [Values(true, false)] bool tapGated,
    [Values(true, false)] bool tapRenewed
  )
  {
    var cycle = Cycle(coCosts, balanced, lifeBalanced, productive, gated, tapGated, tapRenewed);
    var vector = ComboAxisVector.FromCycle(cycle);
    var rendered = ComboPlainLanguage.Describe(vector);
    var engineReason = cycle.LimitingReason;

    var shape =
      $"coCosts={coCosts} balanced={balanced} life={lifeBalanced} productive={productive} "
      + $"gated={gated} tapGated={tapGated} tapRenewed={tapRenewed}";

    if (vector.AllHold)
    {
      // Every edge in these synthetic cycles is GREEN, so all-axes-hold ⇒ the cycle is GREEN ⇒ the
      // engine reports no reason at all.
      Assert.Multiple(() =>
      {
        Assert.That(rendered, Is.EqualTo("certified infinite"), shape);
        Assert.That(engineReason, Is.Null, $"[{shape}] all axes hold but the engine named a reason");
      });
      return;
    }

    Assert.That(engineReason, Is.Not.Null, $"[{shape}] an axis failed but the engine named no reason");
    Assert.That(
      ReasonToSentence.ContainsKey(engineReason!),
      Is.True,
      $"[{shape}] the engine reported a cycle-level LimitingReason '{engineReason}' that "
        + "ComboPlainLanguage has no sentence for — the two implementations have drifted; add the "
        + "sentence (and, if it is a new axis, add it to ComboPlainLanguage.Axes and to the "
        + "combo-axis-expectations.json vocabulary)."
    );
    Assert.That(
      rendered,
      Is.EqualTo(ReasonToSentence[engineReason!]),
      $"[{shape}] engine says '{engineReason}' but the renderer said '{rendered}'"
    );
  }

  /// <summary>Non-vacuity for the table above: every sentence the renderer can produce for a failing
  /// vector is reachable from some engine reason, and vice-versa. A mapping half of which is dead code
  /// would let the metamorphic test pass while covering nothing.</summary>
  [Test]
  public void The_sentence_table_is_a_bijection_with_no_dead_entries()
  {
    string[] renderable =
    [
      ComboPlainLanguage.Gated,
      ComboPlainLanguage.TapNotRenewed,
      ComboPlainLanguage.ManaNegative,
      ComboPlainLanguage.LifeNegative,
      ComboPlainLanguage.NetZero,
      ComboPlainLanguage.UnfedCoCost,
    ];

    Assert.Multiple(() =>
    {
      Assert.That(ReasonToSentence.Values, Is.EquivalentTo(renderable));
      Assert.That(
        renderable.Distinct(StringComparer.Ordinal).Count(),
        Is.EqualTo(renderable.Length),
        "two axes render the same sentence — a reader could not tell them apart"
      );
      Assert.That(ComboPlainLanguage.Axes, Has.Count.EqualTo(5));
    });
  }
}
