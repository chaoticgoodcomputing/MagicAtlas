using Flowthru.Step;
using MagicAST;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// Builds the per-card node metadata for the viz: every distinct card that appears as a node in the
/// union graph (i.e. shows up in <see cref="CardEdgeRow"/>), paired with its oracle text for the
/// hover. Reads the already-materialized edges (no re-parse) + the card inputs; the oracle text is
/// kept verbatim (original newlines preserved — the viz turns them into line breaks).
/// </summary>
[FlowthruStep]
public static class PortNodesStep
{
  public static Func<
    (IEnumerable<CardEdgeRow> Edges, IEnumerable<MastCardInput> CardInputs),
    IEnumerable<PortNodeRow>
  > Create() =>
    inputs =>
    {
      var oracle = new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var ci in inputs.CardInputs)
        oracle.TryAdd(ci.Input.Name, ResolveOracleText(ci.Input));

      return inputs
        .Edges.SelectMany(e => new[] { e.FromCard, e.ToCard })
        .Distinct(StringComparer.Ordinal)
        .Select(card => new PortNodeRow
        {
          Card = card,
          OracleText = oracle.GetValueOrDefault(card, ""),
        })
        .ToList();
    };

  private static string ResolveOracleText(CardInputDTO card)
  {
    if (!string.IsNullOrWhiteSpace(card.OracleText))
      return card.OracleText;
    if (card.CardFaces is { Count: > 0 })
      return string.Join(
        "\n\n",
        card.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0)
      );
    return "";
  }
}
