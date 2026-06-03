using Flowthru.Step;
using MagicAST.Interaction;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// Source step: flattens the authored known-families grammar into <see cref="LabelEdgeRow"/>s — the
/// abstract label-level interaction graph (port label → port label on a resource/family). Feeds the
/// left subplot of the Plotly viz.
/// </summary>
[FlowthruStep]
public static class LabelEdgesStep
{
  public static Func<IEnumerable<LabelEdgeRow>> Create(string grammarPath) =>
    () =>
      FamilyGrammar
        .Load(grammarPath)
        .Select(e => new LabelEdgeRow
        {
          From = e.From,
          To = e.To,
          Resource = e.Resource.ToString(),
          Family = e.Family.ToString(),
        })
        .ToList();
}
