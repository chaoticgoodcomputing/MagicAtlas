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
}
