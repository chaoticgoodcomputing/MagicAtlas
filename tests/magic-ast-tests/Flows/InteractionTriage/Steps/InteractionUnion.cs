using System.Text.Json;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// The shared L2+ reconstruction over the parse-ready combos: walk <em>every</em> parse-ready card's
/// ports once (a port is a card property, deduped per card — NOT siloed per combo), then run the
/// <see cref="PortGraphEngine"/> ONCE over the whole set. Two steps consume it — MaterializeCardEdges
/// (the flat edge export) and MaterializeCycles (the engine's reconstructed loops with cycle-level
/// verdicts) — so the expensive walk/materialize logic lives in one place.
/// </summary>
internal static class InteractionUnion
{
  internal static (PortGraphEngine Engine, IReadOnlyList<PortEdge> Edges) Materialize(
    IEnumerable<Combo> combos,
    IEnumerable<ParseRecord> records,
    IEnumerable<MastCardInput> cardInputs,
    string ontologyPath,
    bool graftCopies = true
  )
  {
    var fullyParsed = records
      .Where(r => r.TotalAbilities > 0 && r.TotalAbilities == r.ParsedAbilities)
      .Select(r => r.CardName)
      .ToHashSet(StringComparer.Ordinal);

    var byName = new Dictionary<string, CardInputDTO>(StringComparer.Ordinal);
    foreach (var ci in cardInputs)
      byName.TryAdd(ci.Input.Name, ci.Input);

    var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
    var walk = new PortWalk(ontology);
    var engine = new PortGraphEngine(ontology);
    var parser = new OracleParser();
    var graphCache = new Dictionary<string, PortGraph>(StringComparer.Ordinal);

    PortGraph GraphFor(string name)
    {
      if (graphCache.TryGetValue(name, out var cached))
        return cached;

      var graph = new PortGraph();
      if (byName.TryGetValue(name, out var dto))
      {
        var text = dto.OracleText;
        if (string.IsNullOrWhiteSpace(text) && dto.CardFaces is { Count: > 0 })
          text = string.Join(
            "\n\n",
            dto.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0)
          );
        if (!string.IsNullOrWhiteSpace(text))
        {
          var abilities = JsonSerializer.SerializeToNode(
            parser.Parse(text).Output.Abilities,
            MagicASTJsonOptions.Strict
          );
          graph = walk.Project(name, abilities);
        }
      }
      graphCache[name] = graph;
      return graph;
    }

    // The union: every distinct parse-ready card walked once (a port is a card property).
    var allGraphs = combos
      .Where(c => c.Cards.All(card => fullyParsed.Contains(card.Name)))
      .SelectMany(c => c.Cards.Select(card => card.Name))
      .Distinct(StringComparer.Ordinal)
      .Select(GraphFor)
      .ToList();

    return (engine, engine.Materialize(allGraphs, graftCopies));
  }
}
