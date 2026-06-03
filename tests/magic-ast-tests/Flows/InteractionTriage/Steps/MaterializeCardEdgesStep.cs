using System.Text.Json;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAST.Schema;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// The L2+ reconstruction step — the materialized card-level <b>union</b> interaction graph. Projects
/// the ports of <em>every</em> card across the parse-ready combos (a port is a card property, deduped
/// per card — NOT siloed per combo), then runs the engine's materialization ONCE over the whole port
/// set under the known-families grammar. Result: every card-card edge that can hold (the operator's
/// <c>Disjoint</c> prune drops the impossible ones), so cycles form from any closed loop among the
/// recognized ports — not just within a single catalogued combo. Emits one flat
/// <see cref="CardEdgeRow"/> per edge (tier-tagged); the viz finds the elementary cycles over the union.
/// </summary>
/// <remarks>
/// Parsing + projection are cached per distinct card name. Port projection serializes the parser's
/// ability AST to the JSON shape the recognizers match (<see cref="MagicASTJsonOptions.Strict"/>). The
/// materialization is a cartesian per grammar edge (every from-label × to-label), bounded by recognizer
/// coverage and pruned by the operator; widen to sampling/caps if the recognized port set grows large.
/// </remarks>
[FlowthruStep]
public static class MaterializeCardEdgesStep
{
  public static Func<
    (
      IEnumerable<Combo> Combos,
      IEnumerable<ParseRecord> Records,
      IEnumerable<MastCardInput> CardInputs
    ),
    IEnumerable<CardEdgeRow>
  > Create(string grammarPath, string ontologyPath) =>
    inputs =>
    {
      var fullyParsed = inputs
        .Records.Where(r => r.TotalAbilities > 0 && r.TotalAbilities == r.ParsedAbilities)
        .Select(r => r.CardName)
        .ToHashSet(StringComparer.Ordinal);

      var byName = new Dictionary<string, CardInputDTO>(StringComparer.Ordinal);
      foreach (var ci in inputs.CardInputs)
        byName.TryAdd(ci.Input.Name, ci.Input);

      var grammar = FamilyGrammar.Load(grammarPath);
      var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
      var projector = new PortProjector(SchemaExport.Build());
      var engine = new InteractionEngine(ontology);
      var parser = new OracleParser();

      var portCache = new Dictionary<string, IReadOnlyList<Port>>(StringComparer.Ordinal);

      IReadOnlyList<Port> PortsFor(string name)
      {
        if (portCache.TryGetValue(name, out var cached))
          return cached;

        IReadOnlyList<Port> ports = [];
        if (byName.TryGetValue(name, out var dto))
        {
          var text = dto.OracleText;
          if (string.IsNullOrWhiteSpace(text) && dto.CardFaces is { Count: > 0 })
          {
            text = string.Join(
              "\n\n",
              dto.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0)
            );
          }
          if (!string.IsNullOrWhiteSpace(text))
          {
            var abilities = JsonSerializer.SerializeToNode(
              parser.Parse(text).Output.Abilities,
              MagicASTJsonOptions.Strict
            );
            ports = projector.Project(name, abilities);
          }
        }
        portCache[name] = ports;
        return ports;
      }

      // The union port set: every distinct card across parse-ready combos, projected once (a port
      // is a card property — deduped here, not re-derived per combo).
      var allPorts = inputs
        .Combos.Where(c => c.Cards.All(card => fullyParsed.Contains(card.Name)))
        .SelectMany(c => c.Cards.Select(card => card.Name))
        .Distinct(StringComparer.Ordinal)
        .SelectMany(PortsFor)
        .ToList();

      // ONE materialization over the whole set → the union card-card graph.
      return engine
        .Materialize(allPorts, grammar)
        .Select(e => new CardEdgeRow
        {
          FromCard = e.From.Card,
          FromLabel = e.From.Label,
          ToCard = e.To.Card,
          ToLabel = e.To.Label,
          Resource = e.Resource.ToString(),
          Family = e.Family.ToString(),
          Tier = e.Tier.ToString(),
          Reason = e.Reason ?? "",
        })
        .ToList();
    };
}
