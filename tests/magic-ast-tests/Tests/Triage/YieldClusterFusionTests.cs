using MagicAST;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Flows.Common;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Clustering;

namespace MagicAtlas.Ast.Tests.Tests.Triage;

/// <summary>
/// Guards the interaction-value fusion in <see cref="YieldClusterAnalyzer"/> (the
/// pick surface's <c>fusedScore</c> ranking). Two load-bearing invariants:
/// <list type="number">
///   <item><b>Graceful degradation</b> — with no combo-value map, <c>FusedScore</c>
///   equals <c>FractionalYield</c> for every cluster and the ranking is identical to
///   the pre-fusion order. The fused surface must never rank <i>worse</i> than
///   parse-yield-only.</item>
///   <item><b>Value re-ranking + fractional attribution</b> — a low-parse-yield
///   surface that unblocks popular combos can outrank a higher-parse-yield surface
///   that unblocks none; a card split across two templates donates its combo value
///   1/N to each (matching the fractional-yield attribution).</item>
/// </list>
/// </summary>
[TestFixture]
public class YieldClusterFusionTests
{
  private static readonly IReadOnlySet<string> NoHandParsed = new HashSet<string>();

  private static CardInputDTO Input(string name) =>
    new() { Name = name, TypeLine = "Instant" };

  /// <summary>One unparsed line carrying a single (pattern, rule) diagnostic.</summary>
  private static LineOutcome UnparsedLine(int index, string text) =>
    new()
    {
      LineIndex = index,
      OracleLine = text,
      Patterns = new List<string> { "P" },
      Diagnostics = new List<LineDiagnostic> { new() { Pattern = "P", LastAttemptedRule = "R" } },
    };

  private static ParseRecord Card(string name, params string[] unparsedLines) =>
    new()
    {
      ScryfallId = name,
      CardName = name,
      Input = Input(name),
      TotalAbilities = unparsedLines.Length,
      ParsedAbilities = 0,
      Lines = unparsedLines.Select((t, i) => UnparsedLine(i, t)).ToList(),
      Residuals = new List<ResidualKindCount>(),
    };

  private const string CounterLine = "Counter target creature spell.";
  private const string DrawLine = "Draw two cards.";

  [Test]
  public void EmptyValueMap_FusedScoreEqualsFractionalYield_AndOrderIsUnchanged()
  {
    // Three cards share the "counter" template (fractionalYield 3.0); one card
    // carries the "draw" template alone (fractionalYield 1.0).
    var records = new[]
    {
      Card("Alpha", CounterLine),
      Card("Bravo", CounterLine),
      Card("Charlie", CounterLine),
      Card("Delta", DrawLine),
    };

    var clusters = YieldClusterAnalyzer.ComputeTopYieldClusters(records, batchSize: 50, NoHandParsed);

    Assert.That(clusters, Has.Count.EqualTo(2));
    foreach (var c in clusters)
    {
      Assert.That(c.FusedScore, Is.EqualTo(c.FractionalYield).Within(1e-9),
        $"FusedScore must equal FractionalYield with no value map (template: {c.Template})");
      Assert.That(c.InteractionValueScore, Is.EqualTo(0.0).Within(1e-9));
      Assert.That(c.ComboPopularityMass, Is.EqualTo(0.0).Within(1e-9));
      Assert.That(c.ComboBlockedCount, Is.EqualTo(0.0).Within(1e-9));
    }

    // Ranking is the pre-fusion fractional-yield order: counter (3.0) then draw (1.0).
    Assert.That(clusters[0].FractionalYield, Is.GreaterThan(clusters[1].FractionalYield));
    Assert.That(clusters[0].Template, Is.EqualTo(clusters.OrderByDescending(c => c.FractionalYield).First().Template));
  }

  [Test]
  public void ComboValue_LiftsLowYieldSurfaceAboveHigherYieldSurface()
  {
    var records = new[]
    {
      Card("Alpha", CounterLine),
      Card("Bravo", CounterLine),
      Card("Charlie", CounterLine), // counter template: fractionalYield 3.0, no combo value
      Card("Delta", DrawLine), // draw template: fractionalYield 1.0, huge combo value
    };

    // Delta gates many popular combos; the counter cards gate none.
    var value = new Dictionary<string, CardComboValue>(StringComparer.Ordinal)
    {
      ["Delta"] = new(BlockedComboCount: 40, PopularityMass: 1_000_000),
    };

    var clusters = YieldClusterAnalyzer.ComputeTopYieldClusters(
      records,
      batchSize: 50,
      NoHandParsed,
      value
    );

    var draw = clusters.Single(c => c.Template == clusters.Single(x => x.ComboPopularityMass > 0).Template);
    var counter = clusters.Single(c => c.ComboPopularityMass == 0.0);

    // Draw: fractionalYield 1.0 × (1 + log10(1 + 1e6)) ≈ 1 × 7 = 7.0 > counter's 3.0.
    Assert.That(draw.ComboPopularityMass, Is.EqualTo(1_000_000.0).Within(1e-6));
    Assert.That(draw.ComboBlockedCount, Is.EqualTo(40.0).Within(1e-9));
    Assert.That(draw.FusedScore, Is.GreaterThan(counter.FusedScore),
      "a combo-unblocking surface must outrank a higher-parse-yield surface with no combo value");
    Assert.That(clusters[0].Template, Is.EqualTo(draw.Template),
      "the combo-unblocking surface should now rank first");
  }

  private static ParseRecord CardWithResiduals(string name, params string[] fragments) =>
    new()
    {
      ScryfallId = name,
      CardName = name,
      Input = Input(name),
      TotalAbilities = 1,
      ParsedAbilities = 1,
      Lines = new List<LineOutcome>(),
      Residuals = new List<ResidualKindCount>(),
      ResidualFragments = fragments.ToList(),
    };

  [Test]
  public void ResidualClusters_GroupByFragmentTemplate_AndFuseComboValue()
  {
    // Three cards carry the same effect fragment across different shells; one carries
    // a different fragment. They must cluster by normalized template, not by card.
    var records = new[]
    {
      CardWithResiduals("Alpha", "create a 1/1 Beast creature token."),
      CardWithResiduals("Bravo", "create a 2/2 Zombie creature token."),
      CardWithResiduals("Charlie", "create a 3/3 Elephant creature token."),
      CardWithResiduals("Delta", "you win the game."),
    };
    var value = new Dictionary<string, CardComboValue>(StringComparer.Ordinal)
    {
      ["Delta"] = new(BlockedComboCount: 5, PopularityMass: 1_000_000),
    };

    var clusters = YieldClusterAnalyzer.ComputeTopResidualClusters(records, depth: 50, value);

    var createToken = clusters.Single(c => c.Template.Contains("create"));
    Assert.That(createToken.FragmentCount, Is.EqualTo(3), "the three token fragments collapse to one family");
    Assert.That(createToken.CardCount, Is.EqualTo(3));

    var win = clusters.Single(c => c.Template.Contains("win"));
    // 1 fragment but huge combo mass → fused score can rival the 3-fragment family.
    Assert.That(win.ComboPopularityMass, Is.EqualTo(1_000_000.0).Within(1e-6));
    Assert.That(win.FusedScore, Is.GreaterThan(win.FragmentCount),
      "combo value boosts the fused score above raw fragment count");
  }

  [Test]
  public void ComboValue_IsAttributedFractionallyAcrossACardsTemplates()
  {
    // One card with two distinct unparsed templates. Its combo value must split
    // 1/2 to each template — the same attribution fractionalYield uses.
    var records = new[] { Card("Echo", CounterLine, DrawLine) };

    var value = new Dictionary<string, CardComboValue>(StringComparer.Ordinal)
    {
      ["Echo"] = new(BlockedComboCount: 10, PopularityMass: 2_000),
    };

    var clusters = YieldClusterAnalyzer.ComputeTopYieldClusters(
      records,
      batchSize: 50,
      NoHandParsed,
      value
    );

    Assert.That(clusters, Has.Count.EqualTo(2));
    foreach (var c in clusters)
    {
      Assert.That(c.FractionalYield, Is.EqualTo(0.5).Within(1e-9),
        "a card across two templates contributes 0.5 fractional yield to each");
      Assert.That(c.ComboPopularityMass, Is.EqualTo(1_000.0).Within(1e-6),
        "combo mass splits 1/2 across the card's two templates");
      Assert.That(c.ComboBlockedCount, Is.EqualTo(5.0).Within(1e-9),
        "blocked-combo count splits 1/2 across the card's two templates");
    }
  }
}
