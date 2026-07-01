using System.Text.Json;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
// Alias: the step's own namespace segment (...Flows.PortGraphAtlas) shadows the report TYPE.
using PortGraphAtlasReport = MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas.PortGraphAtlas;

namespace MagicAtlas.Ast.Tests.Flows.PortGraphAtlas.Steps;

/// <summary>
/// Materializes the EMERGENT port-label graph over the CSB combo-card union and analyzes its EDGE
/// STRUCTURE — the measurement the node-side <see cref="Data._08_Reporting.Schemas.PortLabelCensus"/>
/// and the size-only <see cref="Data._08_Reporting.Schemas.PortGraphMetrics"/> don't do. Answers the
/// two questions from the "atoms of gameplay" framing (<c>libs/mast-interaction/docs/two-layer-cycle-engine.md</c>):
/// <list type="number">
///   <item>Is the label graph one giant SCC like the card projection — and if so, is the blob glued by
///         the universal "economy" connectors (mana/tap)? (Tarjan SCC + a cut-and-re-decompose experiment.)</item>
///   <item>Which short elementary label-cycles bridge ≥2 resource families? Those are the candidate NOVEL
///         combo archetypes — the global generator the anchored DiceComboReport lacks. (Bounded cycle enum.)</item>
/// </list>
/// Self-contained + offline: parses the combo cards straight from the committed <c>CardInputs</c> (no
/// Scryfall fetch, no ParseRecords dependency), mirroring <c>InteractionUnion.GraphFor</c>'s
/// parse → serialize(Strict) → Project idiom. Diagnostic; never a gate.
/// </summary>
[FlowthruStep]
public static class PortGraphAtlasStep
{
  // Bounded elementary-cycle enumeration: the union label graph is dense (hub resources interconnect
  // heavily), so even LENGTH-bounded enumeration explores an exponential path space between closures —
  // same blowup MaterializeCyclesStep bounds its Layer-1 pass against. THREE guards keep it a tractable,
  // honestly-labeled SAMPLE: a length bound, a found-cycle cap, and a HARD DFS-expansion budget (the
  // wall-clock guarantee — the found-cycle cap alone doesn't bound the work spent between closures).
  private const int CycleLenBound = 5;
  private const int MaxCyclesCollected = 40_000;
  private const long MaxDfsExpansions = 6_000_000;

  // The fragmentation probe cuts the top-K highest-degree HUBS (data-driven — the connectors the graph
  // actually has, discovered from the degree distribution, not a guessed family). If the giant SCC
  // shatters into recognizable families, those hubs were its glue.
  private const int HubCutCount = 20;

  private const string ScopeLabel =
    "parse-ready CSB combo-card union (cards of combos whose every card parses cleanly; parsed on-the-fly, offline)";

  public static Func<
    (IEnumerable<Combo> Combos, IEnumerable<MastCardInput> CardInputs),
    PortGraphAtlasReport
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

      // Parse each distinct combo card ONCE (cached — a port is a card property). Mirrors
      // InteractionUnion: the union is the cards of combos whose cards ALL parse cleanly, which keeps the
      // materialize tractable (parse coverage ~40% ⇒ a 3-card combo is all-parsed ~6% of the time, so the
      // parse-ready-combo card set is hundreds, not the full multi-thousand combo-card union). A card is
      // "parse-ready" iff it projects ≥1 port and none is the coarse emit:unparsed coverage marker.
      var graphCache = new Dictionary<string, PortGraph>(StringComparer.Ordinal);
      PortGraph GraphFor(string name)
      {
        if (graphCache.TryGetValue(name, out var g))
          return g;
        g = byName.TryGetValue(name, out var dto) ? ProjectGraph(name, dto, parser, walk) : new PortGraph();
        graphCache[name] = g;
        return g;
      }
      bool ParseReady(string name)
      {
        var g = GraphFor(name);
        return g.Ports.Count > 0
          && !g.Ports.Any(p => p.Label.StartsWith("emit:unparsed", StringComparison.Ordinal));
      }

      var graphs = inputs
        .Combos.Where(c => c.Cards.All(card => ParseReady(card.Name)))
        .SelectMany(c => c.Cards.Select(card => card.Name))
        .Distinct(StringComparer.Ordinal)
        .Select(GraphFor)
        .ToList();

      var sw = System.Diagnostics.Stopwatch.StartNew();

      // ── Build the label graph WITHOUT the per-card-pair instance blowup. A label edge A→B exists iff SOME
      // (A-port, B-port) bonds — but bonding depends only on a port's (label, side, subject, gating), NOT on
      // which of 1,000 identical mana dorks carries it. So we materialize over DEDUPED ports, not the
      // 3,333-card union whose all-pairs cross-product was ~4×10^7 instance edges collapsed to ~10^3 label
      // edges (the 6,000× waste that cost 14 min). Two passes:
      //   A. per-card materialize → the EXACT intra-card edges (wiring consume→emit + same-card self-arms);
      //   B. ONE materialize over a single representative per distinct port SIGNATURE (each on its own
      //      synthetic card, so cross-card arms fire) → the cross-card arm edges, over-approximating
      //      same-as-cross (a SOUND over-approximation for Layer-1 candidates — two-layer-cycle-engine.md).
      var adj = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
      var radj = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
      var cardsPerLabel = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

      void Note(string label, string card)
      {
        if (!cardsPerLabel.TryGetValue(label, out var set))
          cardsPerLabel[label] = set = new HashSet<string>(StringComparer.Ordinal);
        set.Add(card);
      }
      void AddEdge(string a, string b)
      {
        if (string.Equals(a, b, StringComparison.Ordinal))
          return; // a same-label self-hop is not a structural edge in the atom graph
        if (!adj.TryGetValue(a, out var outs))
          adj[a] = outs = new HashSet<string>(StringComparer.Ordinal);
        outs.Add(b);
        if (!radj.TryGetValue(b, out var ins))
          radj[b] = ins = new HashSet<string>(StringComparer.Ordinal);
        ins.Add(a);
      }

      // Centroid masses come from the REAL ports (before dedup): how many in-scope cards carry each label.
      foreach (var g in graphs)
        foreach (var p in g.Ports)
          Note(p.Label, p.Card);

      // Pass A — exact intra-card edges. Each card's own small graph materialized ALONE (no cross-card
      // pairs), so wiring + same-card self-referential arms land precisely. Cheap: O(cards × ports²).
      foreach (var g in graphs)
        foreach (var e in engine.Materialize(new[] { g }))
          AddEdge(e.From.Label, e.To.Label);

      // Pass B — cross-card arm edges over one representative per distinct port signature. The signature is
      // everything the arm operator reads; identical-signature ports bond identically, so one stands in for
      // all N cards that carry it. Unique synthetic cards ⇒ every rep pair is "cross-card" (same-card arms
      // already caught in Pass A), and the engine's per-pair feasibility now runs over ~10^3 reps, not ~10^4
      // ports across 3,333 cards.
      var reps = new Dictionary<string, PortNode>(StringComparer.Ordinal);
      var syn = 0;
      foreach (var g in graphs)
        foreach (var p in g.Ports)
        {
          var sig = $"{p.Label}|{p.Side}|{p.Gated}|{p.TapGated}|{SubjectSig(p.Subject)}";
          if (!reps.ContainsKey(sig))
          {
            var card = "syn::" + syn++;
            reps[sig] = p with { Card = card, Identity = card + "::" + p.Label };
          }
        }
      foreach (var e in engine.Materialize(new[] { new PortGraph { Ports = reps.Values.ToList() } }))
        AddEdge(e.From.Label, e.To.Label);

      var nodes = cardsPerLabel.Keys.ToList();
      var labelEdges = adj.Values.Sum(s => s.Count);
      var cardsInScope = graphs.Count;
      Console.Error.WriteLine(
        $"[PortGraphAtlas] {graphs.Count} cards → {reps.Count} distinct port sigs; "
          + $"{nodes.Count} label nodes, {labelEdges} label edges ({sw.ElapsedMilliseconds} ms)"
      );

      // ── SCC of the full graph (the "is it one blob?" read). ──
      var sccs = Tarjan(nodes, adj);
      var largest = sccs.OrderByDescending(c => c.Count).FirstOrDefault() ?? new List<string>();
      Console.Error.WriteLine(
        $"[PortGraphAtlas] {nodes.Count} label nodes, {labelEdges} edges, {sccs.Count} SCCs, largest={largest.Count} ({sw.ElapsedMilliseconds} ms)"
      );

      // Cycle enumeration runs ONLY over intra-SCC edges — an elementary cycle lives entirely within one
      // strongly-connected component, so every cross-SCC "bridge" edge is dead weight in the DFS. Pruning
      // them (Johnson's decomposition, two-layer doc §Complexity) is the cheap, correctness-preserving cut.
      var compOf = new Dictionary<string, int>(StringComparer.Ordinal);
      for (var ci = 0; ci < sccs.Count; ci++)
        foreach (var n in sccs[ci])
          compOf[n] = ci;
      var intraSccAdj = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
      foreach (var (from, outs) in adj)
      {
        var kept = outs.Where(t => compOf[from] == compOf[t]).ToHashSet(StringComparer.Ordinal);
        if (kept.Count > 0)
          intraSccAdj[from] = kept;
      }

      // ── Hubs: highest total (in+out) degree — the universal connectors. ──
      var topHubs = nodes
        .Select(n => new LabelDegree
        {
          Label = n,
          Family = FamilyOf(n),
          InDegree = radj.TryGetValue(n, out var i) ? i.Count : 0,
          OutDegree = adj.TryGetValue(n, out var o) ? o.Count : 0,
          CardsInScope = cardsPerLabel[n].Count,
        })
        .OrderByDescending(d => d.InDegree + d.OutDegree)
        .ThenByDescending(d => d.CardsInScope)
        .ThenBy(d => d.Label, StringComparer.Ordinal)
        .Take(15)
        .ToList();

      // ── Hub-cut experiment: drop the top-K highest-degree HUBS (data-driven), re-decompose. If the giant
      // SCC shatters, those hubs were its glue — the families that actually connect everything. ──
      int Degree(string n) =>
        (radj.TryGetValue(n, out var i) ? i.Count : 0) + (adj.TryGetValue(n, out var o) ? o.Count : 0);
      var cut = nodes
        .OrderByDescending(Degree)
        .ThenBy(n => n, StringComparer.Ordinal)
        .Take(HubCutCount)
        .ToHashSet(StringComparer.Ordinal);
      var keptNodes = nodes.Where(n => !cut.Contains(n)).ToList();
      var cutAdj = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
      foreach (var (from, outs) in adj)
      {
        if (cut.Contains(from))
          continue;
        var kept = outs.Where(t => !cut.Contains(t)).ToHashSet(StringComparer.Ordinal);
        if (kept.Count > 0)
          cutAdj[from] = kept;
      }
      var cutSccs = Tarjan(keptNodes, cutAdj);
      var islands = cutSccs
        .Where(c => c.Count >= 2)
        .OrderByDescending(c => c.Count)
        .ThenBy(c => c[0], StringComparer.Ordinal)
        .Take(30)
        .Select(c => new LabelIsland
        {
          Size = c.Count,
          DominantFamily = c.GroupBy(FamilyOf)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .First()
            .Key,
          Labels = c.OrderBy(l => l, StringComparer.Ordinal).Take(12).ToList(),
        })
        .ToList();

      // ── Cross-family cycles: bounded elementary label-cycles, tagged by families bridged. ──
      var rings = EnumerateBoundedCycles(nodes, intraSccAdj);
      Console.Error.WriteLine(
        $"[PortGraphAtlas] enumerated {rings.Count} bounded (≤{CycleLenBound}) cycles ({sw.ElapsedMilliseconds} ms)"
      );
      var tagged = rings
        .Select(r => (Ring: r, Families: r.Select(FamilyOf).Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal).ToList()))
        .ToList();
      var crossFamily = tagged.Where(t => t.Families.Count >= 2).ToList();

      // Collapse the facet-variant multiplicity: thousands of cycles that differ only in a token subtype or
      // an ltb scope are ONE archetype. Group by family-SIGNATURE (the sorted family set); each group is a
      // distinct combo shape, reported with a representative ring + how many variant cycles it absorbed.
      var archetypes = crossFamily
        .GroupBy(t => string.Join(", ", t.Families), StringComparer.Ordinal)
        .Select(grp =>
        {
          var example = grp.OrderBy(t => t.Ring.Count)
            .ThenBy(t => string.Join("|", t.Ring), StringComparer.Ordinal)
            .First();
          return new ArchetypeCycle
          {
            Length = example.Ring.Count,
            FamilyCount = example.Families.Count,
            Occurrences = grp.Count(),
            Families = grp.Key,
            Ring = string.Join(" → ", example.Ring) + " → " + example.Ring[0],
          };
        })
        .ToList();
      var sample = archetypes
        .OrderByDescending(a => a.FamilyCount)
        .ThenByDescending(a => a.Occurrences)
        .ThenBy(a => a.Families, StringComparer.Ordinal)
        .Take(25)
        .ToList();

      return new PortGraphAtlasReport
      {
        GeneratedAt = DateTime.UtcNow,
        Scope = ScopeLabel,
        CardsInScope = cardsInScope,
        LabelNodes = nodes.Count,
        LabelEdges = labelEdges,
        EmitNodes = nodes.Count(n => n.StartsWith("emit:", StringComparison.Ordinal)),
        ConsumeNodes = nodes.Count(n => !n.StartsWith("emit:", StringComparison.Ordinal)),
        SccCount = sccs.Count,
        LargestSccSize = largest.Count,
        LargestScc = largest.OrderBy(l => l, StringComparer.Ordinal).Take(40).ToList(),
        TopHubs = topHubs,
        CutFamilies = string.Join(
          ", ",
          cut.Select(FamilyOf).Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal)
        ),
        CutLabelCount = cut.Count,
        SccCountAfterCut = cutSccs.Count,
        LargestSccSizeAfterCut = cutSccs.Count == 0 ? 0 : cutSccs.Max(c => c.Count),
        IslandsAfterCut = islands,
        CycleLenBound = CycleLenBound,
        BoundedCyclesFound = rings.Count,
        CrossFamilyCycles = crossFamily.Count,
        SingleFamilyCycles = rings.Count - crossFamily.Count,
        DistinctArchetypes = archetypes.Count,
        SampleArchetypes = sample,
      };
    };

  /// <summary>A canonical signature of a port's SUBJECT filter for dedup — two ports with the same
  /// (label, side, gating, subject-sig) bond identically, so one represents all cards that carry it.</summary>
  private static string SubjectSig(ObjectFilter? subject) =>
    subject is null ? "" : JsonSerializer.Serialize(subject);

  /// <summary>Parse a card's oracle text and project its port graph (InteractionUnion.GraphFor idiom).</summary>
  private static PortGraph ProjectGraph(string name, CardInputDTO dto, OracleParser parser, PortWalk walk)
  {
    var text = dto.OracleText;
    if (string.IsNullOrWhiteSpace(text) && dto.CardFaces is { Count: > 0 })
      text = string.Join("\n\n", dto.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0));
    if (string.IsNullOrWhiteSpace(text))
      return new PortGraph();
    var abilities = JsonSerializer.SerializeToNode(
      parser.Parse(text).Output.Abilities,
      MagicASTJsonOptions.Strict
    );
    return walk.Project(name, abilities);
  }

  /// <summary>The resource FAMILY a label belongs to — the axis cross-family cycles are measured on. Groups
  /// an emit + its matching trigger/cost onto the same flowing resource (emit:rolldice &amp; trigger:rolldice
  /// → "dice"; emit:mana &amp; pay:mana → "mana"), so a cycle staying within one resource is a single-family
  /// engine and a cycle bridging resources (blink→etb→dice) is a cross-family combo.</summary>
  private static string FamilyOf(string label)
  {
    var parts = label.Split(':');
    var role = parts[0];
    return role switch
    {
      "emit" => ResourceFamily(parts.Length > 1 ? parts[1] : "emit"),
      "trigger" => ResourceFamily(parts.Length > 1 ? parts[1] : "trigger"),
      "pay" => "mana",
      "tap" => "tap",
      "sac" => "sacrifice",
      "etb" => "etb",
      "ltb" => "death",
      "at" => "phase",
      "cast" => "cast",
      "attacksorblocks" => "combat",
      "replace" => "replacement",
      "intercept" => "intercept",
      _ => role,
    };
  }

  private static string ResourceFamily(string kind) =>
    kind switch
    {
      "rolldice" => "dice",
      "additionalcombat" => "combat",
      "returntobattlefield" => "recur",
      "returntohand" => "recur",
      "damage" => "damage",
      "life" => "life",
      "mana" => "mana",
      "token" => "token",
      "counter" => "counter",
      "blink" => "blink",
      "copy" => "copy",
      "cast" => "cast",
      "untap" => "untap",
      _ => kind,
    };

  /// <summary>Tarjan strongly-connected components over a string-node adjacency (iterative-safe at
  /// atom scale — a few hundred nodes). Returns one list per component (singletons included).</summary>
  private static List<List<string>> Tarjan(
    IReadOnlyList<string> nodes,
    IReadOnlyDictionary<string, HashSet<string>> adj
  )
  {
    var index = new Dictionary<string, int>(StringComparer.Ordinal);
    var low = new Dictionary<string, int>(StringComparer.Ordinal);
    var onStack = new HashSet<string>(StringComparer.Ordinal);
    var stack = new Stack<string>();
    var components = new List<List<string>>();
    var next = 0;

    void StrongConnect(string v)
    {
      index[v] = low[v] = next++;
      stack.Push(v);
      onStack.Add(v);
      if (adj.TryGetValue(v, out var outs))
        foreach (var w in outs)
        {
          if (!index.ContainsKey(w))
          {
            StrongConnect(w);
            low[v] = Math.Min(low[v], low[w]);
          }
          else if (onStack.Contains(w))
          {
            low[v] = Math.Min(low[v], index[w]);
          }
        }

      if (low[v] == index[v])
      {
        var component = new List<string>();
        string w;
        do
        {
          w = stack.Pop();
          onStack.Remove(w);
          component.Add(w);
        } while (!string.Equals(w, v, StringComparison.Ordinal));
        components.Add(component);
      }
    }

    foreach (var n in nodes)
      if (!index.ContainsKey(n))
        StrongConnect(n);
    return components;
  }

  /// <summary>Bounded elementary-cycle enumeration over the label graph, canonically rooted at each
  /// cycle's minimum-ordinal node (so each elementary cycle is found once — the same discipline as the
  /// engine's <c>LabelCycleHops</c>). Length-bounded + collection-capped because the union label graph is
  /// dense; the result is an honest SAMPLE, not the (intractable) complete set.</summary>
  private static List<List<string>> EnumerateBoundedCycles(
    IReadOnlyList<string> nodes,
    IReadOnlyDictionary<string, HashSet<string>> adj
  )
  {
    var rings = new List<List<string>>();
    var path = new List<string>();
    var onPath = new HashSet<string>(StringComparer.Ordinal);
    var expansions = 0L; // HARD wall-clock budget: DFS steps, not just closures found

    bool Budget() => rings.Count >= MaxCyclesCollected || expansions >= MaxDfsExpansions;

    void Dfs(string start, string node)
    {
      if (Budget() || !adj.TryGetValue(node, out var outs))
        return;
      foreach (var to in outs)
      {
        if (Budget())
          return;
        expansions++;
        if (string.Equals(to, start, StringComparison.Ordinal))
        {
          rings.Add(new List<string>(path)); // closed an elementary cycle rooted at start
        }
        else if (
          path.Count < CycleLenBound
          && string.CompareOrdinal(to, start) > 0
          && !onPath.Contains(to)
        )
        {
          path.Add(to);
          onPath.Add(to);
          Dfs(start, to);
          onPath.Remove(to);
          path.RemoveAt(path.Count - 1);
        }
      }
    }

    foreach (var start in nodes.OrderBy(n => n, StringComparer.Ordinal))
    {
      if (Budget())
        break;
      path.Clear();
      onPath.Clear();
      path.Add(start);
      onPath.Add(start);
      Dfs(start, start);
    }

    if (expansions >= MaxDfsExpansions)
      Console.Error.WriteLine(
        $"[PortGraphAtlas] cycle enumeration hit the {MaxDfsExpansions:N0}-expansion budget — sample is partial (dense SCC)."
      );
    return rings;
  }
}
