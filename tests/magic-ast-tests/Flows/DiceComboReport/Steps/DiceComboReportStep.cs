using System.Text.Json;
using System.Text.Json.Nodes;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using ReportSchema = MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas.DiceComboReport;

namespace MagicAtlas.Ast.Tests.Flows.DiceComboReport.Steps;

/// <summary>
/// Reconstructs every CSB die-roll combo "as if the support cards were parsed" and reports the best
/// dice-producing cycle per combo + the engine-derived (novel) dice loops. AST provenance per card:
/// gold fixture &gt; hand-authored stub (dice-report-stub-asts.json) &gt; parsed oracle text &gt; inert.
/// Reconstruction mirrors the bench (per-combo <see cref="PortGraphEngine.FindCycles"/>), but at a
/// GENEROUS reach so long loops surface — each row flags whether the loop fits the product reach (6).
/// </summary>
[FlowthruStep]
public static class DiceComboReportStep
{
  // Generous per-combo enumeration reach. The combo graphs are tiny (2–5 cards), so unbounded-ish
  // enumeration is tractable; we report each loop's hop count against the product reach (6) explicitly.
  private const int ReportReach = 14;

  public static Func<
    (IEnumerable<Combo> Combos, IEnumerable<MastCardInput> CardInputs),
    ReportSchema
  > Create(string ontologyPath, string goldFixturesRoot, string stubAstsPath) =>
    inputs =>
    {
      var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
      var walk = new PortWalk(ontology);
      var engine = new PortGraphEngine(ontology);
      var parser = new OracleParser();

      var golds = LoadGolds(goldFixturesRoot); // name -> Oracle.Abilities (trusted hand-parsed AST)
      var stubs = LoadStubs(stubAstsPath); // name -> Oracle.Abilities (report-only flow-relevant stub)
      var text = inputs
        .CardInputs.GroupBy(c => c.Input.Name, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.First().Input.OracleText ?? "", StringComparer.Ordinal);

      var sourceOf = new Dictionary<string, string>(StringComparer.Ordinal);
      var graphCache = new Dictionary<string, PortGraph>(StringComparer.Ordinal);

      PortGraph GraphFor(string name)
      {
        if (graphCache.TryGetValue(name, out var cached))
          return cached;
        JsonNode? abilities = null;
        string source;
        if (golds.TryGetValue(name, out var g))
        {
          abilities = g;
          source = "gold";
        }
        else if (stubs.TryGetValue(name, out var s))
        {
          abilities = s;
          source = "stub";
        }
        else if (text.TryGetValue(name, out var t) && !string.IsNullOrWhiteSpace(t))
        {
          abilities = JsonSerializer.SerializeToNode(
            parser.Parse(t).Output.Abilities,
            MagicASTJsonOptions.Strict
          );
          source = "parsed";
        }
        else
          source = "inert";

        var graph = abilities is null ? new PortGraph() : walk.Project(name, abilities);
        if (graph.Ports.Count == 0)
          source = "inert"; // present but contributes no flow ports (e.g. an enabler, or empty parse)
        sourceOf[name] = source;
        graphCache[name] = graph;
        return graph;
      }

      var allCombos = inputs.Combos.ToList();
      var diceCombos = allCombos
        .Where(c => c.Results.Any(r => r.Contains("die roll", StringComparison.OrdinalIgnoreCase)
          || r.Contains("dice", StringComparison.OrdinalIgnoreCase)))
        .OrderBy(c => c.Id, StringComparer.Ordinal)
        .ToList();

      // CSB cross-check index (over ALL combos): card -> combos containing it, for verified/partial/derived.
      var comboCards = allCombos
        .Select(c => (c.Id, Cards: c.Cards.Select(x => x.Name).ToHashSet(StringComparer.Ordinal)))
        .ToList();
      var cardToCombo = new Dictionary<string, List<int>>(StringComparer.Ordinal);
      for (var i = 0; i < comboCards.Count; i++)
        foreach (var card in comboCards[i].Cards)
          (cardToCombo.TryGetValue(card, out var l) ? l : cardToCombo[card] = []).Add(i);

      (string Match, string ComboId) Classify(IReadOnlyCollection<string> cards)
      {
        var cardSet = cards.ToHashSet(StringComparer.Ordinal);
        var candidates = new HashSet<int>();
        foreach (var card in cards)
          if (cardToCombo.TryGetValue(card, out var cs))
            candidates.UnionWith(cs);
        if (candidates.Count == 0)
          return ("derived", "");
        var best = candidates
          .Select(i => (Index: i, Overlap: comboCards[i].Cards.Count(cardSet.Contains), Size: comboCards[i].Cards.Count))
          .OrderByDescending(x => x.Overlap)
          .ThenBy(x => x.Size)
          .ThenBy(x => x.Index)
          .First();
        if (best.Overlap < 2)
          return ("derived", "");
        if (best.Overlap == cards.Count && best.Size == cards.Count)
          return ("verified", comboCards[best.Index].Id);
        return ("partial", comboCards[best.Index].Id);
      }

      static IReadOnlyList<string> CardsOf(PortCycle c) =>
        c.Edges.SelectMany(e => new[] { e.From.Card, e.To.Card }).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();

      // A cycle "drives a roll" iff a port on its ring has a card-defined edge to an emit:rolldice, OR a
      // rolldice port sits directly on the ring — i.e. each loop iteration produces a die roll.
      bool DrivesRoll(PortCycle c, IReadOnlyList<PortEdge> edges)
      {
        if (c.Edges.Any(e => e.From.Label.StartsWith("emit:rolldice", StringComparison.Ordinal)
          || e.To.Label.StartsWith("trigger:rolldice", StringComparison.Ordinal)
          || e.From.Label.StartsWith("trigger:rolldice", StringComparison.Ordinal)))
          return true;
        var ring = c.Edges.SelectMany(e => new[] { e.From.Identity, e.To.Identity }).ToHashSet(StringComparer.Ordinal);
        return edges.Any(e => e.Provenance == EdgeProvenance.CardDefined
          && ring.Contains(e.From.Identity)
          && e.To.Label.StartsWith("emit:rolldice", StringComparison.Ordinal));
      }

      var rows = new List<DiceComboRow>();
      var novel = new List<DiceCycleRow>();
      var novelSeen = new HashSet<string>(StringComparer.Ordinal);
      var reconstructedAny = 0;
      var reconstructedWithinReach = 0;

      foreach (var combo in diceCombos)
      {
        var names = combo.Cards.Select(c => c.Name).ToList();
        var graphs = names.Select(GraphFor).ToList();
        var edges = engine.Materialize(graphs);
        var cycles = engine.FindCycles(edges, ReportReach);
        var diceCycles = cycles.Where(c => DrivesRoll(c, edges)).ToList();

        // Best dice cycle: prefer a MULTI-card loop (a 1-card self-loop is the dice engine but not a
        // multi-card combo — the bench drops 1-card loops; CR has no 1-card combo), then lowest tier,
        // then fewest hops. A combo whose ONLY dice cycle is 1-card falls back to it (with a note).
        var best = diceCycles
          .OrderBy(c => CardsOf(c).Count == 1 ? 1 : 0)
          .ThenBy(c => (int)c.Tier)
          .ThenBy(c => c.Edges.Count)
          .FirstOrDefault();

        var sources = names.Select(n => $"{n}={sourceOf.GetValueOrDefault(n, "?")}").ToList();

        if (best is null)
        {
          rows.Add(new DiceComboRow
          {
            Id = combo.Id, Cards = names, Results = combo.Results,
            BestDiceCycleTier = "none", BestDiceCycleHops = 0, WithinProductReach = false,
            CardsInCycle = [], Classification = "none", CardAstSources = sources,
            Note = "no dice-producing cycle — the loop's driver isn't reconstructed by current arms",
          });
          continue;
        }

        reconstructedAny++;
        var hops = best.Edges.Count;
        var withinReach = hops <= PortGraphEngine.DefaultReconstructionReach;
        if (withinReach)
          reconstructedWithinReach++;
        var cardsInCycle = CardsOf(best);
        var (match, comboId) = Classify(cardsInCycle);
        var note = cardsInCycle.Count == 1
          ? "1-card self-loop (the dice engine lives on one card); the combo's other cards are payoffs/enablers off the cycle"
          : best.LimitingReason ?? "reconstructed";
        if (!withinReach)
          note = $"loop is {hops} hops > product reach {PortGraphEngine.DefaultReconstructionReach} — exists in the graph but the product enumerator won't surface it; " + note;

        rows.Add(new DiceComboRow
        {
          Id = combo.Id, Cards = names, Results = combo.Results,
          BestDiceCycleTier = best.Tier.ToString(), BestDiceCycleHops = hops, WithinProductReach = withinReach,
          CardsInCycle = cardsInCycle, Classification = match, CardAstSources = sources, Note = note,
        });

        // Novel-loop scan: any dice cycle whose card set is NOT exactly a CSB combo (a simpler subset the
        // engine derived, or a cross-card loop CSB doesn't list). Dedup by card set across all combos.
        foreach (var c in diceCycles)
        {
          var cards = CardsOf(c);
          if (cards.Count < 2)
            continue;
          var (m, cid) = Classify(cards);
          if (m == "verified")
            continue; // exactly a known combo — not novel
          var key = string.Join("|", cards);
          if (!novelSeen.Add(key))
            continue;
          novel.Add(new DiceCycleRow
          {
            Cards = cards, Tier = c.Tier.ToString(), Hops = c.Edges.Count, Classification = m, ComboId = cid,
            Ring = c.Edges.Select(e => $"{e.From.Card}:{e.From.Label}").ToList(),
          });
        }
      }

      return new ReportSchema
      {
        GeneratedAt = DateTime.UtcNow,
        TotalDiceCombos = diceCombos.Count,
        ReconstructedAny = reconstructedAny,
        ReconstructedWithinReach = reconstructedWithinReach,
        ProductReach = PortGraphEngine.DefaultReconstructionReach,
        Combos = rows,
        NovelLoops = novel.OrderBy(n => (int)Enum.Parse<CertaintyTier>(n.Tier)).ThenBy(n => n.Hops).ToList(),
      };
    };

  private static Dictionary<string, JsonNode> LoadGolds(string root)
  {
    var map = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
    if (!Directory.Exists(root))
      return map;
    foreach (var file in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
    {
      JsonNode? node;
      try { node = JsonNode.Parse(File.ReadAllText(file)); }
      catch { continue; }
      var name = node?["Input"]?["Name"]?.GetValue<string>();
      if (string.IsNullOrEmpty(name) || map.ContainsKey(name))
        continue;
      if (node?["Output"]?["Oracle"]?["Abilities"] is JsonArray abilities)
        map[name] = abilities.DeepClone();
    }
    return map;
  }

  private static Dictionary<string, JsonNode> LoadStubs(string path)
  {
    var map = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
    if (!File.Exists(path))
      return map;
    if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject obj)
      return map;
    foreach (var (k, v) in obj)
      if (!k.StartsWith('_') && v is JsonArray arr)
        map[k] = arr.DeepClone();
    return map;
  }
}
