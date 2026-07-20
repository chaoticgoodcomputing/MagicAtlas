using MagicAST.AST.References;
using MagicAST.Interaction;
using NUnit.Framework;

namespace MagicAtlas.Bench.Tests;

/// <summary>
/// <see cref="LimitingHopSummary.FromWorst"/> is a <b>second implementation</b> of
/// <see cref="PortCycle.LimitingHop"/> — it reduces the already-projected <see cref="HopDiagnostic"/> list
/// so the pinned <c>expected</c> block stays a pure projection of <see cref="ComboDiagnostics"/>. That is a
/// reasonable design, but a duplicated rule with only a doc comment asserting the duplication is faithful
/// is exactly the ADR-0004 failure mode, and it already happened once: the comment claimed it picked "the
/// SAME way" long after <see cref="PortCycle.LimitingHop"/> gained its null-when-nothing-limits rule.
///
/// <para>This is the metamorphic check that makes the claim executable — not "what is the right hop?", but
/// "do the two implementations agree?". It runs the same cycle through both paths over the full
/// (tier-shape × §8-flag) space, so any future divergence fails here rather than silently re-pinning every
/// GREEN combo to an alphabetical accident.</para>
/// </summary>
[TestFixture]
public class LimitingHopAgreesWithEngineTest
{
  private static PortNode Port(string card, string label, PortSide side) =>
    new()
    {
      Card = card,
      Label = label,
      Side = side,
      Identity = $"{card}::{label}",
    };

  private static PortEdge Edge(string fromLabel, FilterRelation overlap, Trilean reliability) =>
    new()
    {
      From = Port("A", fromLabel, PortSide.Emit),
      To = Port("B", "consume:whatever", PortSide.Consume),
      Provenance = EdgeProvenance.RulesDefined,
      Overlap = overlap,
      Reliability = reliability,
    };

  /// <summary>GREEN, AMBER and RED edge shapes, deliberately labelled so that the alphabetically-first
  /// label is NOT the worst-tier one — a selection that fell back to the name tie-break would disagree.</summary>
  private static readonly (string Name, PortEdge[] Edges)[] Shapes =
  [
    ("empty", []),
    ("single green", [Edge("emit:aaa", FilterRelation.Overlaps, Trilean.Yes)]),
    (
      "all green, multiple",
      [
        Edge("emit:zzz", FilterRelation.Overlaps, Trilean.Yes),
        Edge("emit:aaa", FilterRelation.Overlaps, Trilean.Yes),
      ]
    ),
    (
      "green + amber (amber sorts last)",
      [
        Edge("emit:aaa", FilterRelation.Overlaps, Trilean.Yes),
        Edge("emit:zzz", FilterRelation.Overlaps, Trilean.Unknown),
      ]
    ),
    (
      "green + amber + red (red sorts last)",
      [
        Edge("emit:aaa", FilterRelation.Overlaps, Trilean.Yes),
        Edge("emit:mmm", FilterRelation.Overlaps, Trilean.Unknown),
        Edge("emit:zzz", FilterRelation.Disjoint, Trilean.No),
      ]
    ),
    (
      "two ambers — the tie-break is load-bearing here and must match",
      [
        Edge("emit:zzz", FilterRelation.Overlaps, Trilean.Unknown),
        Edge("emit:aaa", FilterRelation.Overlaps, Trilean.Unknown),
      ]
    ),
  ];

  [Test]
  public void The_bench_projection_and_the_engine_select_the_same_limiting_hop(
    [ValueSource(nameof(ShapeNames))] string shapeName,
    [Values(true, false)] bool balanced,
    [Values(true, false)] bool productive
  )
  {
    var edges = Shapes.First(s => s.Name == shapeName).Edges;
    // Balanced/Productive are the §8 flags that floor a cycle to AMBER without any hop being worse than
    // GREEN — the case where the two implementations most easily disagree. (Firable is computed from the
    // edges, so it is not a free variable here.)
    var cycle = new PortCycle
    {
      Edges = edges,
      Balanced = balanced,
      Productive = productive,
    };

    var engine = cycle.LimitingHop;
    var projected = LimitingHopSummary.FromWorst(ComboDiagnostics.FromCycle(cycle).Edges);

    if (engine is null)
    {
      Assert.That(
        projected,
        Is.Null,
        $"[{shapeName}] engine reports NO limiting hop but the bench projection reported "
          + $"{projected?.From} → {projected?.To} — the two implementations have drifted"
      );
      return;
    }

    Assert.That(projected, Is.Not.Null, $"[{shapeName}] engine selected a limiting hop, bench reported none");
    Assert.Multiple(() =>
    {
      Assert.That(projected!.From, Is.EqualTo(engine.From.Label), $"[{shapeName}] From");
      Assert.That(projected.To, Is.EqualTo(engine.To.Label), $"[{shapeName}] To");
      Assert.That(projected.Reliability, Is.EqualTo(engine.Reliability.ToString()), $"[{shapeName}] Reliability");
    });
  }

  public static IEnumerable<string> ShapeNames() => Shapes.Select(s => s.Name);
}
