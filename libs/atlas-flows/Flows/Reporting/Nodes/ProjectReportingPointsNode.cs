using Flowthru.Step;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Flows.Reporting.Nodes;

/// <summary>
/// Strips the embedding-specific <c>text_type</c> field from <see cref="AtlasPoint"/> rows,
/// producing the model-agnostic <see cref="ReportingPoint"/> shape the Reporting flow joins
/// against card metadata. Multiple atlas points per card (one per fragment) are preserved — the
/// Plotly step shows each fragment as its own dot.
/// </summary>
[FlowthruStep]
public static class ProjectReportingPointsNode
{
  public static Func<
    IEnumerable<AtlasPoint>,
    Task<IEnumerable<ReportingPoint>>
  > Create() =>
    points =>
      Task.FromResult<IEnumerable<ReportingPoint>>(
        points
          .Select(p => new ReportingPoint
          {
            PointId = p.PointId,
            CardId = p.CardId,
            X = p.X,
            Y = p.Y,
          })
          .ToList()
      );
}
