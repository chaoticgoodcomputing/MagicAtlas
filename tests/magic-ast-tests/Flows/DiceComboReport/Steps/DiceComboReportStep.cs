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

      // The "string off a loop" model. A core loop produces a die roll EACH ITERATION iff a roll EMITTER
      // (emit:rolldice) is reachable from the loop's ports by following emit->consume / card-defined edges
      // forward — the roll need NOT be a load-bearing hop ON the ring. Returns whether a roll is produced,
      // whether it sits ON the ring (load-bearing) vs hangs off as an OFFSHOOT (a roll-on-ETB card riding
      // an event the loop spins), and the offshoot card + hop distance from the ring.
      static bool IsRoll(string l) => l.StartsWith("emit:rolldice", StringComparison.Ordinal);
      (bool Produces, bool OnRing, string Card, int Distance) RollAttachment(
        PortCycle c, IReadOnlyList<PortEdge> edges)
      {
        var ringPorts = c.Edges.SelectMany(e => new[] { e.From, e.To }).ToList();
        var onRing = ringPorts.FirstOrDefault(p => IsRoll(p.Label));
        if (onRing is not null)
          return (true, true, onRing.Card, 0); // the roll feeds a hop the loop needs (load-bearing)

        // Forward BFS from the ring over ALL edges: the loop's per-iteration output reaching a rolldice
        // emitter on a DIFFERENT (off-ring) port is the offshoot — a string, not part of the loop.
        var adj = edges
          .GroupBy(e => e.From.Identity, StringComparer.Ordinal)
          .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var seen = ringPorts.Select(p => p.Identity).ToHashSet(StringComparer.Ordinal);
        var q = new Queue<(string Id, int D)>(ringPorts.Select(p => (p.Identity, 0)));
        while (q.Count > 0)
        {
          var (id, d) = q.Dequeue();
          if (!adj.TryGetValue(id, out var outs))
            continue;
          foreach (var e in outs)
          {
            if (IsRoll(e.To.Label))
              return (true, false, e.To.Card, d + 1);
            if (seen.Add(e.To.Identity))
              q.Enqueue((e.To.Identity, d + 1));
          }
        }
        return (false, false, "", 0);
      }

      var rows = new List<DiceComboRow>();
      var novel = new List<DiceCycleRow>();
      var novelSeen = new HashSet<string>(StringComparer.Ordinal);
      var reconstructedAny = 0;
      var reconstructedWithinReach = 0;

      // Analyze one card set: find the SMALLEST core loop whose forward closure produces a roll (so the
      // roll may ride as an offshoot), report it, and collect any novel (non-CSB-combo) dice loops.
      DiceComboRow AnalyzeCombo(string id, IReadOnlyList<string> names, IReadOnlyList<string> results, bool collectNovel)
      {
        var graphs = names.Select(GraphFor).ToList();
        var edges = engine.Materialize(graphs);
        var cycles = engine.FindCycles(edges, ReportReach);
        var producing = cycles
          .Select(c => (Cycle: c, Roll: RollAttachment(c, edges)))
          .Where(x => x.Roll.Produces)
          .ToList();
        var sources = names.Select(n => $"{n}={sourceOf.GetValueOrDefault(n, "?")}").ToList();

        // Prefer the SMALLEST CORE loop (fewest cards) — the efficient engine, with the roll riding off it
        // — then lowest tier, then fewest hops. (A small core + an offshoot roll beats a big roll-on-loop.)
        var best = producing
          .OrderBy(x => CardsOf(x.Cycle).Count)
          .ThenBy(x => (int)x.Cycle.Tier)
          .ThenBy(x => x.Cycle.Edges.Count)
          .FirstOrDefault();

        if (collectNovel)
          foreach (var x in producing)
          {
            var cards = CardsOf(x.Cycle);
            if (cards.Count < 2)
              continue;
            var (m, cid) = Classify(cards);
            if (m == "verified" || !novelSeen.Add(string.Join("|", cards)))
              continue;
            novel.Add(new DiceCycleRow
            {
              Cards = cards, Tier = x.Cycle.Tier.ToString(), Hops = x.Cycle.Edges.Count,
              Classification = m, ComboId = cid,
              Ring = x.Cycle.Edges.Select(e => $"{e.From.Card}:{e.From.Label}").ToList(),
            });
          }

        if (best.Cycle is null)
          return new DiceComboRow
          {
            Id = id, Cards = names, Results = results, BestDiceCycleTier = "none", BestDiceCycleHops = 0,
            WithinProductReach = false, CardsInCycle = [], Classification = "none", RollAttachment = "none",
            CardAstSources = sources, CoreRing = [],
            Note = "no dice-producing loop — no core cycle whose closure reaches a roll emitter",
          };

        var hops = best.Cycle.Edges.Count;
        var withinReach = hops <= PortGraphEngine.DefaultReconstructionReach;
        var coreCards = CardsOf(best.Cycle);
        var (match, comboId) = Classify(coreCards);
        var attach = best.Roll.OnRing
          ? "on-loop (load-bearing rolldice hop on the ring)"
          : $"offshoot via {best.Roll.Card} (+{best.Roll.Distance} hops off the core loop)";
        var note = best.Roll.OnRing
          ? best.Cycle.LimitingReason ?? "roll is load-bearing on the core loop"
          : $"the {coreCards.Count}-card core loop spins an event each iteration; {best.Roll.Card}'s roll is a SECONDARY effect riding it (a string off the loop), not part of the cycle";
        if (!withinReach)
          note = $"core loop is {hops} hops > product reach {PortGraphEngine.DefaultReconstructionReach}; " + note;

        return new DiceComboRow
        {
          Id = id, Cards = names, Results = results, BestDiceCycleTier = best.Cycle.Tier.ToString(),
          BestDiceCycleHops = hops, WithinProductReach = withinReach, CardsInCycle = coreCards,
          Classification = match, RollAttachment = attach, CardAstSources = sources, Note = note,
          CoreRing = best.Cycle.Edges.Select(e => $"{e.From.Card}:{e.From.Label}").ToList(),
        };
      }

      foreach (var combo in diceCombos)
        rows.Add(AnalyzeCombo(combo.Id, combo.Cards.Select(c => c.Name).ToList(), combo.Results, collectNovel: true));

      // ANCHORED efficient-engine scan (the user's "string off a loop" hypothesis, made concrete): a
      // minimal infinite-ETB/mana core engine + a roll-on-ETB card as a pure offshoot. Emiel the Blessed
      // (blink outlet) + Peregrine Drake (ETB untap 5 lands = net-positive mana) is THE classic 2-card
      // infinite-mana/ETB loop; blinking a roll-on-ETB creature alongside it rolls every iteration for free.
      var efficient = new List<DiceComboRow>();
      var candidates = new (string Id, string[] Cards)[]
      {
        ("engine:emiel+drake+swarming", ["Emiel the Blessed", "Peregrine Drake", "Swarming Goblins"]),
        ("engine:emiel+drake (control, no roll card)", ["Emiel the Blessed", "Peregrine Drake"]),
        ("engine:emiel+swarming (no mana source)", ["Emiel the Blessed", "Swarming Goblins"]),
        // Second newly-covered activated blink outlet (returnToBattlefield ExiledWith:Self → emit:blink).
        ("engine:eldrazi-displacer+drake+swarming", ["Eldrazi Displacer", "Peregrine Drake", "Swarming Goblins"]),
      };
      foreach (var (id, cards) in candidates)
        efficient.Add(AnalyzeCombo(id, cards, ["(anchored efficient-engine candidate)"], collectNovel: false));

      // Counts over the CSB combos only (not the anchored candidates).
      reconstructedAny = rows.Count(r => r.BestDiceCycleTier != "none");
      reconstructedWithinReach = rows.Count(r => r.WithinProductReach);

      return new ReportSchema
      {
        GeneratedAt = DateTime.UtcNow,
        TotalDiceCombos = diceCombos.Count,
        ReconstructedAny = reconstructedAny,
        ReconstructedWithinReach = reconstructedWithinReach,
        ProductReach = PortGraphEngine.DefaultReconstructionReach,
        Combos = rows,
        NovelLoops = novel.OrderBy(n => (int)Enum.Parse<CertaintyTier>(n.Tier)).ThenBy(n => n.Hops).ToList(),
        EfficientEngines = efficient,
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
