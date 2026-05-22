using Flowthru.Step;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Flows.Reporting.Nodes;

/// <summary>
/// Identity projection from <see cref="AtlasPoint"/> to <see cref="ReportingPoint"/>. The shapes
/// are intentionally identical today — keeping the projection step gives reporting a stable
/// view that survives upstream schema tweaks (e.g. when a future variant adds columns to
/// <c>AtlasPoint</c>, the reporting layer ignores them by construction).
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
            LineId = p.LineId,
            X = p.X,
            Y = p.Y,
          })
          .ToList()
      );
}
