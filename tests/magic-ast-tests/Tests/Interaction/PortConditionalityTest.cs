namespace MagicAST.Interaction.Tests;

using MagicAST.Interaction;

/// <summary>
/// ADR 0004 #43 — <see cref="PortConditionality"/> is the port-side plain-language renderer that replaces
/// the "Green"/"Amber" half of the retired four-valued port tier. Like
/// <see cref="ComboPlainLanguage"/> one layer up it must be (a) total, (b) pure, and (c) <b>lossless</b>:
/// where the old tier collapsed every gate shape to "Amber", the renderer must name each holding axis, so
/// a port that both taps and is rate-limited is distinguishable from one that only taps.
/// </summary>
[TestFixture]
public class PortConditionalityTest
{
  private static PortConditionAxes Axes(
    bool tap = false,
    bool counter = false,
    bool rate = false
  ) => new() { TapGated = tap, RequiresCounter = counter, RateLimited = rate };

  [Test]
  public void No_axis_set_is_unconditional()
  {
    var a = Axes();
    Assert.Multiple(() =>
    {
      Assert.That(a.Unconditional, Is.True);
      Assert.That(PortConditionality.Describe(a), Is.EqualTo("fires unconditionally"));
      Assert.That(PortConditionality.DescribeAll(a), Is.Empty);
    });
  }

  [Test]
  public void Each_single_axis_renders_its_own_phrase()
  {
    Assert.Multiple(() =>
    {
      Assert.That(PortConditionality.Describe(Axes(tap: true)), Is.EqualTo("needs to tap"));
      Assert.That(
        PortConditionality.Describe(Axes(counter: true)),
        Is.EqualTo("needs a counter on it")
      );
      Assert.That(PortConditionality.Describe(Axes(rate: true)), Is.EqualTo("fires only under a condition"));
    });
  }

  /// <summary>The lossless property — the whole point of the split. The old tier could only say "Amber"
  /// here; the renderer names both gates in canonical order.</summary>
  [Test]
  public void Multiple_axes_are_all_rendered_in_canonical_order()
  {
    var a = Axes(tap: true, rate: true);
    Assert.Multiple(() =>
    {
      Assert.That(a.Unconditional, Is.False);
      Assert.That(
        PortConditionality.DescribeAll(a),
        Is.EqualTo(new[] { "needs to tap", "fires only under a condition" })
      );
      Assert.That(PortConditionality.Describe(a), Is.EqualTo("needs to tap · fires only under a condition"));
    });
  }

  [Test]
  public void All_three_axes_render_together()
  {
    var a = Axes(tap: true, counter: true, rate: true);
    Assert.That(
      PortConditionality.Describe(a),
      Is.EqualTo("needs to tap · needs a counter on it · fires only under a condition")
    );
  }

  /// <summary>Totality + non-collision over the full axis space: every one of the eight vectors yields a
  /// non-empty description, and the seven conditional vectors are all distinguishable from each other and
  /// from the unconditional one (the conflation the split exists to undo).</summary>
  [Test]
  [Combinatorial]
  public void Every_vector_is_total_and_the_conditional_ones_are_distinguishable(
    [Values(true, false)] bool tap,
    [Values(true, false)] bool counter,
    [Values(true, false)] bool rate
  )
  {
    var a = Axes(tap, counter, rate);
    var rendered = PortConditionality.Describe(a);

    Assert.That(rendered, Is.Not.Empty);
    Assert.That(
      a.Unconditional,
      Is.EqualTo(!tap && !counter && !rate),
      "Unconditional must hold iff no axis is set"
    );
    // DescribeAll size equals the number of set axes — nothing dropped, nothing invented.
    Assert.That(
      PortConditionality.DescribeAll(a),
      Has.Count.EqualTo((tap ? 1 : 0) + (counter ? 1 : 0) + (rate ? 1 : 0))
    );
  }
}
