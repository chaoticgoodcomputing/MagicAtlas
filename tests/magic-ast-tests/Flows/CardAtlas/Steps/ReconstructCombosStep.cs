using System.Text.Json;
using Flowthru.Step;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAST;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.CardAtlas.Steps;

/// <summary>
/// D4 — ComboInstances. For every parse-ready CSB combo, reconstructs the interaction cycles the engine
/// finds among its cards' parsed ports (PER-COMBO — each combo graph is 2–5 cards, so the materialize is
/// tiny and the 847s whole-union blowup is avoided) and emits one row per (combo, distinct
/// family-signature cycle): named cards + certainty tier + firability + the CSB-declared result. This is
/// the "shape → buildable" payoff — <c>DiceComboReport</c> generalised beyond dice to every family. An
/// exploiter anchors on a family (sacrifice) by filtering <see cref="ComboInstanceRow.FamilySignature"/>.
/// </summary>
[FlowthruStep]
public static class ReconstructCombosStep
{
  public static Func<
    (IEnumerable<Combo> Combos, IEnumerable<MastCardInput> CardInputs),
    IEnumerable<ComboInstanceRow>
  > Create(string ontologyPath) =>
    inputs =>
    {
      var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
      var walk = new PortWalk(ontology);
      var engine = new PortGraphEngine(ontology);
      var parser = new OracleParser();

      var byName = new Dictionary<string, CardInputDTO>(StringComparer.Ordinal);
      foreach (var ci in inputs.CardInputs)
        byName.TryAdd(ci.Input.Name, ci.Input);

      var graphCache = new Dictionary<string, PortGraph>(StringComparer.Ordinal);
      PortGraph GraphFor(string name)
      {
        if (graphCache.TryGetValue(name, out var g))
          return g;
        g = byName.TryGetValue(name, out var dto)
          ? CardAtlasShared.Project(name, dto, parser, walk)
          : new PortGraph();
        graphCache[name] = g;
        return g;
      }
      bool ParseReady(string name)
      {
        var g = GraphFor(name);
        return g.Ports.Count > 0
          && !g.Ports.Any(p => p.Label.StartsWith("emit:unparsed", StringComparison.Ordinal));
      }

      var rows = new List<ComboInstanceRow>();
      var reconstructed = 0;
      var parseReadyCombos = 0;

      foreach (var combo in inputs.Combos)
      {
        if (combo.Cards.Count < 2 || !combo.Cards.All(c => ParseReady(c.Name)))
          continue;
        parseReadyCombos++;

        var graphs = combo.Cards.Select(c => GraphFor(c.Name)).ToList();
        var edges = engine.Materialize(graphs); // tiny — a single combo's 2–5 cards
        var cycles = engine
          .FindCyclesByLabelGraph(edges)
          .Where(cy =>
            cy.Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
              .Distinct(StringComparer.Ordinal)
              .Count() > 1
          )
          .ToList();
        if (cycles.Count == 0)
          continue;
        reconstructed++;

        var results = string.Join("; ", combo.Results);
        // One row per distinct family-signature the combo realizes; keep the best-tier representative.
        foreach (
          var grp in cycles
            .GroupBy(CardAtlasShared.SignatureOf, StringComparer.Ordinal)
            .Where(g => g.Key.Length > 0)
        )
        {
          var best = grp.OrderBy(cy => (int)cy.Tier).First();
          var cards = CardAtlasShared.CardsOf(best);
          rows.Add(new ComboInstanceRow
          {
            ComboId = combo.Id,
            Cards = string.Join(" + ", cards),
            CardCount = cards.Count,
            FamilySignature = grp.Key,
            FamilyRing = CardAtlasShared.RingOf(best),
            Tier = best.Tier.ToString(),
            Firable = best.Firable,
            Results = results,
            Popularity = combo.Popularity,
          });
        }
      }

      Console.Error.WriteLine(
        $"[ReconstructCombos] {parseReadyCombos} parse-ready combos → {reconstructed} reconstructed → {rows.Count} combo-instance rows"
      );
      return rows;
    };
}
