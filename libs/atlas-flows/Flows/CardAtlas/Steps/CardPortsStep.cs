using System.Text.Json;
using System.Text.RegularExpressions;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Flows.Shared;

namespace MagicAtlas.Flows.CardAtlas.Steps;

/// <summary>
/// D1 — the CardPorts keystone. Over the parse-ready CSB combo-card union, emits the card↔port index
/// (<see cref="CardPortRow"/>: one row per (card, distinct port label), family + emit/consume side) and
/// the per-card deckbuilding metadata (<see cref="CardMetaRow"/>: colour identity + derived mana value +
/// type line). This is the "shape → buildable" bridge both persona reviews found missing: every family
/// station / archetype can now resolve to actual, filterable cards. Metadata comes straight from the
/// committed <c>card-inputs.json</c> (CardInputDTO carries ManaCost / TypeLine / ColorIdentity); price and
/// EDHREC are not in that source (they'd arrive with a fuller Scryfall fetch in atlas-flows). Clean by
/// construction — projects each REAL card's own ports, so none of the materialize's synthetic
/// "copy of X" projection nodes appear.
///
/// <para>Promoted from tests/magic-ast-tests/Flows/CardAtlas/Steps/CardPortsStep.cs.</para>
/// </summary>
[FlowthruStep]
public static partial class CardPortsStep
{
  [GeneratedRegex(@"\{([^}]+)\}")]
  private static partial Regex ManaSymbol();

  public static Func<
    (IEnumerable<Combo> Combos, IEnumerable<MastCardInput> CardInputs),
    (IEnumerable<CardMetaRow>, IEnumerable<CardPortRow>)
  > Create(string ontologyPath) =>
    inputs =>
    {
      var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
      var walk = new PortWalk(ontology);
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

      var unionNames = inputs
        .Combos.Where(c => c.Cards.All(card => ParseReady(card.Name)))
        .SelectMany(c => c.Cards.Select(card => card.Name))
        .Distinct(StringComparer.Ordinal)
        .Where(byName.ContainsKey)
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToList();

      var metas = new List<CardMetaRow>(unionNames.Count);
      var ports = new List<CardPortRow>();
      foreach (var name in unionNames)
      {
        var dto = byName[name];
        var distinctPorts = GraphFor(name)
          .Ports.GroupBy(p => p.Label, StringComparer.Ordinal)
          .Select(g => g.First())
          .OrderBy(p => p.Label, StringComparer.Ordinal)
          .ToList();
        foreach (var p in distinctPorts)
          ports.Add(new CardPortRow
          {
            Card = name,
            Label = p.Label,
            Family = ResourceFamilies.Of(p.Label),
            Side = p.Label.StartsWith("emit:", StringComparison.Ordinal) ? "emit" : "consume",
            OracleLineIndex = p.OracleLineIndex,
            Spans = p.SourceSpan is MagicAST.AST.TextSpan s
              ? new[] { new[] { s.Start, s.End } }
              : null,
          });
        metas.Add(new CardMetaRow
        {
          Card = name,
          ColorIdentity = dto.ColorIdentity is { Count: > 0 } ci ? string.Concat(ci) : "",
          Cmc = DeriveCmc(dto.ManaCost),
          TypeLine = dto.TypeLine,
          PortCount = distinctPorts.Count,
        });
      }

      Console.Error.WriteLine(
        $"[CardPorts] {metas.Count} cards, {ports.Count} card-port rows over the parse-ready combo union"
      );
      return (metas, ports);
    };

  /// <summary>Mana value from a mana-cost string: <c>{3}{G}</c> → 4. Generic <c>{N}</c> adds N; a hybrid /
  /// phyrexian symbol (<c>{2/W}</c>, <c>{W/U}</c>, <c>{W/P}</c>) adds the max of its parts (numeric or 1);
  /// <c>{X}/{Y}/{Z}</c> add 0; any other single symbol (colour / colourless / snow) adds 1.</summary>
  private static int DeriveCmc(string? manaCost)
  {
    if (string.IsNullOrEmpty(manaCost))
      return 0;
    var cmc = 0;
    foreach (Match m in ManaSymbol().Matches(manaCost))
    {
      var s = m.Groups[1].Value;
      if (int.TryParse(s, out var n))
        cmc += n;
      else if (s.Contains('/', StringComparison.Ordinal))
        cmc += s.Split('/').Select(p => int.TryParse(p, out var pn) ? pn : 1).Max();
      else if (s is "X" or "Y" or "Z")
        cmc += 0;
      else
        cmc += 1;
    }
    return cmc;
  }
}
