using System.Text.Json;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.Common;

namespace MagicAtlas.Ast.Tests.Flows.LabelCensus.Steps;

/// <summary>
/// Parses + PortWalk-projects every corpus card, then aggregates the distinct port-label space — the
/// measurement behind the two-layer cycle engine (does the projection collapse cards → a small atom
/// set?). Mirrors <c>InteractionUnion.GraphFor</c>'s parse → serialize(Strict) → Project idiom, but
/// over the whole corpus rather than the combo subset, and counts labels instead of building edges.
/// </summary>
[FlowthruStep]
public static class CensusStep
{
  // Roles that form interaction edges (Materialize / FlowFeasible / the bridges). Everything else
  // (modify, evasion, coarse emit:<x>, unprojected trigger events) is inert — never on a cycle.
  private static readonly HashSet<string> EdgeRoles = new(StringComparer.Ordinal)
  {
    "sac", "pay", "tap", "ltb", "etb", "at", "trigger", "emit", "replace", "intercept",
  };

  // emit sub-kinds that a flow arm actually reads, vs the coarse emit:<x> fallback.
  private static readonly HashSet<string> RealEmitKinds = new(StringComparer.Ordinal)
  {
    "token", "mana", "life", "counter", "untap", "returntobattlefield", "copy",
  };

  public static Func<IEnumerable<MastCardInput>, PortLabelCensus> Create(
    string ontologyPath,
    string? interactionTriageReportPath = null
  ) =>
    cards =>
    {
      var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
      var comboValue = CardComboValueLoader.Load(interactionTriageReportPath);
      var walk = new PortWalk(ontology);
      var parser = new OracleParser();
      var labelToCards = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
      var walked = 0;

      foreach (var ci in cards)
      {
        var name = ci.Input.Name;
        var text = ci.Input.OracleText;
        if (string.IsNullOrWhiteSpace(text) && ci.Input.CardFaces is { Count: > 0 })
          text = string.Join(
            "\n\n",
            ci.Input.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0)
          );
        if (string.IsNullOrWhiteSpace(text))
          continue;

        walked++;
        var abilities = JsonSerializer.SerializeToNode(
          parser.Parse(text).Output.Abilities,
          MagicASTJsonOptions.Strict
        );
        foreach (var port in walk.Project(name, abilities).Ports)
        {
          if (!labelToCards.TryGetValue(port.Label, out var set))
            labelToCards[port.Label] = set = new HashSet<string>(StringComparer.Ordinal);
          set.Add(name);
        }
      }

      static bool CycleRelevant(string label)
      {
        var parts = label.Split(':');
        if (!EdgeRoles.Contains(parts[0]))
          return false;
        if (parts[0] == "emit")
          return parts.Length >= 2 && RealEmitKinds.Contains(parts[1]); // exclude coarse emit:<x>
        return true;
      }

      // A projection gap is a coarse EMIT label — an edge-role emission (emit:<x>
      // fallback or emit:unparsed) that no flow arm reads, so the card projects but
      // forms no edge. These are exactly the labels a new PortWalk arm would light
      // up. (Non-emit edge roles are always cycle-relevant, so they are never gaps.)
      static bool IsProjectionGap(string label) =>
        label.StartsWith("emit:", StringComparison.Ordinal) && !CycleRelevant(label);

      var distinct = labelToCards.Keys.ToList();
      var cycleRelevant = distinct.Count(CycleRelevant);

      var byRole = distinct
        .GroupBy(l => l.Split(':')[0])
        .Select(g => new RoleLabelCount
        {
          Role = g.Key,
          DistinctLabels = g.Count(),
          EdgeForming = EdgeRoles.Contains(g.Key),
        })
        .OrderByDescending(r => r.DistinctLabels)
        .ThenBy(r => r.Role, StringComparer.Ordinal)
        .ToList();

      var topLabels = labelToCards
        .OrderByDescending(kv => kv.Value.Count)
        .ThenBy(kv => kv.Key, StringComparer.Ordinal)
        .Take(20)
        .Select(kv => new LabelCardCount { Label = kv.Key, CardCount = kv.Value.Count })
        .ToList();

      // Projection pick surface: coarse emit labels an edge role would carry but
      // that no flow arm reads (emit:<x> fallbacks + emit:unparsed), ranked by the
      // combo-popularity mass of the cards carrying them. One PortWalk arm projects
      // every card with the label, so the mass is the un-split value a single
      // projection unit unblocks. Cards with no combo value contribute 0.
      var topProjectionGaps = labelToCards
        .Where(kv => IsProjectionGap(kv.Key))
        .Select(kv =>
        {
          var valuedCards = kv.Value
            .Select(name => (name, value: comboValue.GetValueOrDefault(name)))
            .Where(x => x.value is not null)
            .OrderByDescending(x => x.value!.PopularityMass)
            .ToList();
          return new ProjectionGap
          {
            Label = kv.Key,
            CardCount = kv.Value.Count,
            ComboBlockedCards = valuedCards.Count,
            ComboPopularityMass = valuedCards.Sum(x => x.value!.PopularityMass),
            ExampleCards = valuedCards.Take(5).Select(x => x.name).ToList(),
          };
        })
        .OrderByDescending(g => g.ComboPopularityMass)
        .ThenByDescending(g => g.CardCount)
        .ThenBy(g => g.Label, StringComparer.Ordinal)
        .Take(30)
        .ToList();

      return new PortLabelCensus
      {
        GeneratedAt = DateTime.UtcNow,
        CardsWalked = walked,
        DistinctLabels = distinct.Count,
        CycleRelevantLabels = cycleRelevant,
        InertLabels = distinct.Count - cycleRelevant,
        CardsPerDistinctLabel = distinct.Count == 0 ? 0 : Math.Round((double)walked / distinct.Count, 2),
        CardsPerCycleRelevantLabel =
          cycleRelevant == 0 ? 0 : Math.Round((double)walked / cycleRelevant, 2),
        ByRole = byRole,
        TopLabels = topLabels,
        TopProjectionGaps = topProjectionGaps,
      };
    };
}
