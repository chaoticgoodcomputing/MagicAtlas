namespace MagicAtlas.Ast.Tests.Flows.OverApproximation.Steps;

using System.Text.Json;
using System.Text.Json.Nodes;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// ADR-0004 §6 — computes the over-approximation delta: <c>AST condition nodes −
/// conditions the projection consumed</c>, joined to the D1 ports (and their tiers) that consequently
/// rest on each dropped node.
///
/// <para>Scope is the D1 <c>CardPorts</c> card set (the parse-ready CSB combo-card union), so the tier
/// join is total and the report speaks about exactly the ports the atlas publishes. The card's abilities
/// are re-parsed here rather than read from a dataset — the AST is not persisted anywhere, and re-parsing
/// is the same idiom <c>CardAtlasShared.Project</c> uses, which keeps the projection this step ablates
/// bit-identical to the one that produced the ports it joins to.</para>
///
/// <para>No register, no whitelist, no baseline count: every number here is derived from the AST and the
/// live projection. That is the point of §6 — a hand-maintained list of accepted over-approximations
/// would be exactly the drift surface the ADR exists to remove.</para>
/// </summary>
[FlowthruStep]
public static class OverApproximationStep
{
  private const string Note =
    "Over-approximation report (ADR-0004 §6, modeled-dependency completeness). Each row is an AST "
    + "Condition node the PortWalk projection DROPPED — deleting it from the AST leaves the projected "
    + "port graph bit-identical, so nothing in the interaction layer reads it. The ports listed rest on "
    + "that unmodeled condition; the greenPorts subset is the answer to 'which GREENs rest on unmodeled "
    + "conditions'. An over-approximation is LEGAL (the projection over-proposes, the operator prunes, "
    + "ADR-0003 §7) — it must merely be enumerable, which is what this is. Fully derived by ablation: "
    + "there is no hand-maintained register, and a slice that starts consuming a condition drops it from "
    + "this report with no edit. Distinct from known-coarse-projections.json, which names DISCRIMINATORS "
    + "projected coarsely (lost resolution) rather than CONDITION NODES dropped entirely (lost guard).";

  public static Func<
    (IEnumerable<CardPortRow> Ports, IEnumerable<MastCardInput> CardInputs),
    OverApproximationReport
  > Create(string ontologyPath) =>
    inputs =>
    {
      var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
      var walk = new PortWalk(ontology);
      var parser = new OracleParser();

      var byName = new Dictionary<string, CardInputDTO>(StringComparer.Ordinal);
      foreach (var ci in inputs.CardInputs)
        byName.TryAdd(ci.Input.Name, ci.Input);

      // The D1 conditionality index (ADR 0004 #43): (card, label) → conditionality, over PARSED ports only
      // (Provenance == ""). The join that turns "a dropped condition" into "an UNCONDITIONAL port resting on
      // a dropped condition" — the old "GREEN" signal, now read off the split-out conditionality dimension.
      var cond = new Dictionary<(string, string), string>();
      var cardSet = new HashSet<string>(StringComparer.Ordinal);
      foreach (var p in inputs.Ports)
      {
        cardSet.Add(p.Card);
        if (string.IsNullOrEmpty(p.Provenance))
          cond[(p.Card, p.Label)] = p.Conditionality;
      }
      var cards = cardSet.OrderBy(c => c, StringComparer.Ordinal).ToList();

      var total = 0;
      var dropped = new List<DroppedConditionRow>();

      foreach (var card in cards)
      {
        if (!byName.TryGetValue(card, out var dto))
          continue;
        var text = OracleTextOf(dto);
        if (string.IsNullOrWhiteSpace(text))
          continue;

        var abilities =
          JsonSerializer.SerializeToNode(parser.Parse(text).Output.Abilities, MagicASTJsonOptions.Strict)
          as JsonArray;
        if (abilities is null)
          continue;

        var sites = ConditionConsumption.Collect(abilities);
        if (sites.Count == 0)
          continue;
        total += sites.Count;

        foreach (var d in ConditionConsumption.Dropped(walk, card, abilities))
        {
          var greens = d
            .AffectedPortLabels.Where(l =>
              cond.TryGetValue((card, l), out var c)
              && string.Equals(c, PortConditionality.Unconditional, StringComparison.Ordinal)
            )
            .ToList();
          dropped.Add(
            new DroppedConditionRow
            {
              Card = card,
              ConditionType = d.Site.ConditionType,
              Path = d.Site.Path,
              ConditionJson = d.Site.Json,
              OracleClause = Slice(text, d.AbilitySpan),
              AffectedPorts = d.AffectedPortLabels,
              GreenPorts = greens,
            }
          );
        }
      }

      var byType = dropped
        .GroupBy(d => d.ConditionType, StringComparer.Ordinal)
        .Select(g => new DroppedConditionTypeRow
        {
          ConditionType = g.Key,
          DroppedCount = g.Count(),
          CardCount = g.Select(d => d.Card).Distinct(StringComparer.Ordinal).Count(),
          GreenPorts = g.SelectMany(d => d.GreenPorts.Select(l => (d.Card, l))).Distinct().Count(),
          ExampleCard = g.OrderBy(d => d.Card, StringComparer.Ordinal).First().Card,
        })
        .OrderByDescending(r => r.DroppedCount)
        .ThenBy(r => r.ConditionType, StringComparer.Ordinal)
        .ToList();

      return new OverApproximationReport
      {
        GeneratedAt = DateTime.UtcNow.ToString("O"),
        Note = Note,
        CardsScanned = cards.Count,
        ConditionNodesTotal = total,
        ConditionNodesConsumed = total - dropped.Count,
        ConditionNodesDropped = dropped.Count,
        CardsWithDroppedConditions = dropped.Select(d => d.Card).Distinct(StringComparer.Ordinal).Count(),
        GreenPortsOnUnmodeledConditions = dropped
          .SelectMany(d => d.GreenPorts.Select(l => (d.Card, l)))
          .Distinct()
          .Count(),
        AmberPortsOnUnmodeledConditions = dropped
          .SelectMany(d =>
            d.AffectedPorts.Where(l =>
                cond.TryGetValue((d.Card, l), out var c)
                && !string.Equals(c, PortConditionality.Unconditional, StringComparison.Ordinal)
              )
              .Select(l => (d.Card, l))
          )
          .Distinct()
          .Count(),
        ByConditionType = byType,
        Dropped = dropped
          .OrderByDescending(d => d.GreenPorts.Count)
          .ThenBy(d => d.Card, StringComparer.Ordinal)
          .ThenBy(d => d.Path, StringComparer.Ordinal)
          .ToList(),
      };
    };

  /// <summary>The card's oracle text, composing DFC faces exactly as <c>CardAtlasShared.Project</c> does
  /// (so the spans this step slices index the same string the parser saw).</summary>
  private static string OracleTextOf(CardInputDTO dto)
  {
    var text = dto.OracleText;
    if (string.IsNullOrWhiteSpace(text) && dto.CardFaces is { Count: > 0 })
      text = string.Join("\n\n", dto.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0));
    return text ?? "";
  }

  private static string Slice(string text, int[]? span) =>
    span is [var s, var e] && s >= 0 && e <= text.Length && e > s ? text[s..e] : "";
}
