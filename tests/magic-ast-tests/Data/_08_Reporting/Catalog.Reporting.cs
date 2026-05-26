using Flowthru.Data.Catalog;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Data;

/// <summary>Reporting layer: human- and agent-facing summaries.</summary>
public partial class Catalog
{
  /// <summary>
  /// The single output of the <c>MagicAstTriage</c> flow. Consumed directly by
  /// the <c>mast-tdd-loop</c> skill — agents read this file to pick their
  /// assigned gap. Path is the contract; do not rename without updating the
  /// skill.
  /// </summary>
  public IItem<TriageReport> TriageReport =>
    CreateItem(() => Item.Of<TriageReport>("TriageReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/triage-report.json")
      .Build());

  /// <summary>
  /// Discovery-side companion to <see cref="TriageReport"/>. Lexical-template
  /// clustering over unparsed oracle lines + greedy set-cover yield projection.
  /// Surfaces unnamed structural patterns and recommends the highest-yield
  /// K-cluster batch for the next session.
  /// </summary>
  public IItem<YieldClustersReport> YieldClusters =>
    CreateItem(() => Item.Of<YieldClustersReport>("YieldClusters")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/yield-clusters.json")
      .Build());
}
