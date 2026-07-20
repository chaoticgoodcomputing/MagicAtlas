namespace MagicAST.Interaction.Tests;

using MagicAST.AST.References;
using MagicAST.Interaction;

/// <summary>
/// ADR 0004 — <b>a cycle with nothing limiting it has no limiting hop</b>.
/// <para><see cref="PortCycle.LimitingHop"/> used to return the alphabetically-first edge unconditionally.
/// For an all-GREEN cycle every <c>Tier</c> is equal, so the <c>OrderByDescending</c> key is constant and
/// the winner was decided entirely by the <c>ThenBy(From.Label)</c> tie-break — an implementation detail
/// with no semantic content, frozen into <c>combo-expected-tiers.json</c> as though it were a
/// diagnostic.</para>
/// <para>The concrete failure: retiring the sac→death bridge into subsumption (ADR-0003 §5) introduced
/// <c>emit:removal:creature:…</c>, which sorts before <c>emit:token:…</c> ('r' &lt; 't'). Chatterfang ×
/// Pitiless Plunderer stayed GREEN with all six §8 flags unchanged, yet the gate reported that "the REASON
/// it reconstructs at this tier silently changed". These invariants make that unrepresentable.</para>
/// </summary>
[TestFixture]
public class LimitingHopStabilityTest
{
  private static PortNode Port(string card, string label, PortSide side) =>
    new()
    {
      Card = card,
      Label = label,
      Side = side,
      Identity = $"{card}::{label}",
    };

  /// <summary>A rules-defined edge tiered purely by its operator verdicts (post-ADR-0004).</summary>
  private static PortEdge Edge(string fromLabel, FilterRelation overlap, Trilean reliability) =>
    new()
    {
      From = Port("A", fromLabel, PortSide.Emit),
      To = Port("B", "consume:whatever", PortSide.Consume),
      Provenance = EdgeProvenance.RulesDefined,
      Overlap = overlap,
      Reliability = reliability,
    };

  private static PortEdge Green(string fromLabel) =>
    Edge(fromLabel, FilterRelation.Overlaps, Trilean.Yes);

  private static PortEdge Amber(string fromLabel) =>
    Edge(fromLabel, FilterRelation.Overlaps, Trilean.Unknown);

  /// <summary>An all-GREEN cycle: no hop is limiting, so there is no limiting hop to report.</summary>
  [Test]
  public void An_all_green_cycle_has_no_limiting_hop()
  {
    var cycle = new PortCycle { Edges = [Green("emit:token:artifact:treasure"), Green("emit:mana:any")] };

    Assert.Multiple(() =>
    {
      Assert.That(cycle.Tier, Is.EqualTo(CertaintyTier.Green));
      Assert.That(
        cycle.LimitingHop,
        Is.Null,
        "an all-GREEN cycle reported a limiting hop — the alphabetical tie-break is back"
      );
      Assert.That(cycle.LimitingReason, Is.Null, "LimitingHop and LimitingReason must agree");
    });
  }

  /// <summary>
  /// THE REGRESSION. Adding an unrelated GREEN edge that sorts earlier must not change the diagnostic —
  /// this is the exact shape of the sac→death remodel that broke the Chatterfang pin.
  /// </summary>
  [Test]
  public void An_earlier_sorting_green_edge_does_not_change_the_diagnostic()
  {
    var before = new PortCycle { Edges = [Green("emit:token:artifact:treasure:controlled")] };
    var after = new PortCycle
    {
      Edges =
      [
        Green("emit:token:artifact:treasure:controlled"),
        // sorts BEFORE "emit:token:…" — the port the §5 sacrifice remodel introduced.
        Green("emit:removal:creature:squirrel:to-graveyard:sacrificed:controlled"),
      ],
    };

    Assert.Multiple(() =>
    {
      Assert.That(after.Tier, Is.EqualTo(before.Tier), "tier moved — this test's premise is wrong");
      Assert.That(
        after.LimitingHop,
        Is.EqualTo(before.LimitingHop),
        "an unrelated earlier-sorting GREEN edge changed the reported limiting hop"
      );
    });
  }

  /// <summary>
  /// A cycle floored by §8 accounting rather than by a hop: every edge is GREEN, so the limit is a
  /// property of the cycle. <see cref="PortCycle.LimitingReason"/> names it; LimitingHop stays null rather
  /// than blaming an arbitrary edge for the balance failure.
  /// </summary>
  [Test]
  public void A_section8_floored_cycle_blames_the_accounting_not_a_hop()
  {
    var cycle = new PortCycle
    {
      Edges = [Green("emit:mana:any"), Green("emit:token:creature")],
      Balanced = false,
    };

    Assert.Multiple(() =>
    {
      Assert.That(cycle.Tier, Is.EqualTo(CertaintyTier.Amber));
      Assert.That(cycle.LimitingReason, Is.EqualTo("mana-negative"));
      Assert.That(
        cycle.LimitingHop,
        Is.Null,
        "a §8-floored cycle blamed an individual hop for a cycle-level accounting failure"
      );
    });
  }

  /// <summary>
  /// The positive control — without it, returning null unconditionally would pass everything above. A
  /// genuinely worse hop IS still reported, and is selected by tier rather than by name: the AMBER hop
  /// wins even though its label sorts last.
  /// </summary>
  [Test]
  public void A_genuinely_limiting_hop_is_still_reported_and_chosen_by_tier()
  {
    var cycle = new PortCycle
    {
      Edges = [Green("emit:aaa:sorts:first"), Amber("emit:zzz:sorts:last")],
    };

    Assert.Multiple(() =>
    {
      Assert.That(cycle.Tier, Is.EqualTo(CertaintyTier.Amber));
      Assert.That(cycle.LimitingHop, Is.Not.Null);
      Assert.That(
        cycle.LimitingHop!.From.Label,
        Is.EqualTo("emit:zzz:sorts:last"),
        "the limiting hop was chosen alphabetically instead of by tier"
      );
    });
  }
}
